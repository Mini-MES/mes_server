using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class AutomatedSensorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<AutomatedSensorBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public AutomatedSensorBackgroundService(
            IServiceScopeFactory scopeFactory,
            IHubContext<MesHub> hubContext,
            ILogger<AutomatedSensorBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 현장 생산 지시 연동 센서 가동 서비스 시작됨");

            while (!stoppingToken.IsCancellationRequested)
            {
                bool isEnabled = _configuration.GetValue<bool>("SensorSimulation:Enabled", true);
                int intervalSeconds = _configuration.GetValue<int>("SensorSimulation:IntervalSeconds", 3);

                if (isEnabled)
                {
                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                            // 💡 현재 상태가 'RUNNING'인 설비 조회 (CurrentLotId가 비어있으면 진행 중인 active Lot을 자동 할당)
                            var runningEquipments = await dbContext.Equipments
                                .Where(e => e.Status == EquipmentStatus.Running)
                                .ToListAsync(stoppingToken);

                            foreach (var equipment in runningEquipments)
                            {
                                var lotId = equipment.CurrentLotId;
                                if (string.IsNullOrEmpty(lotId))
                                {
                                    var activeLot = await dbContext.Lots
                                        .Where(l => l.Status == mes_server.Models.Enum.LotStatus.WIP || l.Status == mes_server.Models.Enum.LotStatus.RELEASED)
                                        .FirstOrDefaultAsync(stoppingToken);
                                    if (activeLot != null)
                                    {
                                        lotId = activeLot.LotID;
                                        equipment.CurrentLotId = lotId;
                                    }
                                }

                                if (string.IsNullOrEmpty(lotId)) continue;

                                // 💡 해당 Lot의 최근 실적 등록 작업자 또는 DB에 등록된 실제 작업자(Operator/Worker) 조회
                                var lastPerfUser = await dbContext.Performances
                                    .Where(p => p.LotID == lotId)
                                    .OrderByDescending(p => p.WorkDate)
                                    .Select(p => p.UserID)
                                    .FirstOrDefaultAsync(stoppingToken);

                                var operatorUser = lastPerfUser != null 
                                    ? lastPerfUser 
                                    : (await dbContext.Users.FirstOrDefaultAsync(u => u.UserRole == "Operator" || u.UserRole == "Worker", stoppingToken))?.UserID
                                      ?? (await dbContext.Users.FirstOrDefaultAsync(stoppingToken))?.UserID 
                                      ?? "user1";

                                string systemUserId = operatorUser;

                                // 1. 설정된 주기(초)만큼 누적 가동 시간 증가                                                                                                                          
                                equipment.TotalRunningSeconds += intervalSeconds;

                                // DB Lot 및 WorkOrder 조회
                                var lot = await dbContext.Lots
                                    .Include(l => l.WorkOrder)
                                    .FirstOrDefaultAsync(l => l.LotID == lotId, stoppingToken);

                                if (lot != null && lot.WorkOrder != null && lot.WorkOrder.Status == mes_server.Models.Enum.OrderStatus.InProgress)
                                {
                                    var workOrder = lot.WorkOrder;
                                    var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                                    // 센서 실적 1건 등록
                                    var perf = new mes_server.Models.History.Performance
                                    {
                                        WorkOrderID = lot.OrderID,
                                        LotID = lot.LotID,
                                        ProcessID = lot.CurrentProcessID,
                                        UserID = systemUserId,
                                        InputQty = 1,
                                        GoodQty = 1,
                                        BadQty = 0,
                                        WorkDate = DateTime.Now
                                    };
                                    dbContext.Performances.Add(perf);

                                    // 투입 원자재 / 반제품 재고 차감 (-1)
                                    await inventoryService.ConsumeMaterialByProcessAsync(lot.OrderID, lot.CurrentProcessID, 1);

                                    // 현재 공정에서의 누적 양품 실적 수량 계산
                                    var currentProcessGoodCount = await dbContext.Performances
                                        .Where(p => p.LotID == lot.LotID && p.ProcessID == lot.CurrentProcessID)
                                        .SumAsync(p => p.GoodQty, stoppingToken) + 1; // 이번 실적 포함

                                    // 💡 해당 제품(ProductID)의 하위 BOM 계층 전체 공정 목록을 재귀적으로 조회
                                    var productBomProcessIds = await GetAllBomProcessIdsAsync(dbContext, workOrder.ProductID, stoppingToken);

                                    List<ProcessMaster> processes;
                                    if (productBomProcessIds.Any())
                                    {
                                        processes = await dbContext.ProcessMasters
                                            .Where(p => productBomProcessIds.Contains(p.ProcessID))
                                            .OrderBy(p => p.SequenceOrder)
                                            .ToListAsync(stoppingToken);
                                    }
                                    else
                                    {
                                        processes = await dbContext.ProcessMasters
                                            .OrderBy(p => p.SequenceOrder)
                                            .ToListAsync(stoppingToken);
                                    }

                                    // 💡 '출하' 공정은 물류 단계이므로 생산 센서 가동 공정 목록에서 제외
                                    processes = processes
                                        .Where(p => !p.ProcessName.Contains("출하") && !p.ProcessName.Contains("Shipment"))
                                        .ToList();

                                    var currentProcess = processes.FirstOrDefault(p => p.ProcessID == lot.CurrentProcessID);
                                    var nextProcess = currentProcess != null
                                        ? processes.FirstOrDefault(p => p.SequenceOrder > currentProcess.SequenceOrder)
                                        : null;

                                    // 목표 수량(TargetQty) 채워졌는지 검사하여 다음 공정 자동 이동 또는 마감
                                    if (currentProcessGoodCount >= workOrder.TargetQty)
                                    {
                                        if (nextProcess != null)
                                        {
                                            // 1차/중간 반제품 재고 자동 입고 (+TargetQty)
                                            await inventoryService.ReceiveSemiFinishedProductAsync(workOrder.OrderID, lot.CurrentProcessID, workOrder.TargetQty);

                                            lot.CurrentProcessID = nextProcess.ProcessID;
                                            _logger.LogInformation("🔄 [자동 공정 이동] Lot {LotId}: 공정 {From} -> 공정 {To} (반제품 재고 입고 완료)",
                                                lot.LotID, currentProcess?.ProcessName, nextProcess.ProcessName);

                                            await _hubContext.Clients.All.SendAsync("LotUpdated", new
                                            {
                                                lotId = lot.LotID,
                                                nextProcessId = nextProcess.ProcessID
                                            }, stoppingToken);
                                        }
                                        else
                                        {
                                            // 마지막 공정 달성 시 완제품 입고, 작업지시 완료 및 설비 정지
                                            await inventoryService.ReceiveFinishedProductAsync(workOrder.OrderID, workOrder.TargetQty);

                                            workOrder.TotalGoodQty += workOrder.TargetQty;
                                            workOrder.Status = mes_server.Models.Enum.OrderStatus.Completed;
                                            lot.Status = mes_server.Models.Enum.LotStatus.DONE;
                                            equipment.Status = mes_server.Models.MasterData.EquipmentStatus.Idle;
                                            equipment.CurrentLotId = null;

                                            _logger.LogInformation("🎉 [작업 지시 마감] Lot {LotId}, Order {OrderId} 최종 완제품 입고 및 마감 완료!",
                                                lot.LotID, workOrder.OrderID);

                                            await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new { orderId = workOrder.OrderID }, stoppingToken);
                                        }
                                    }
                                }

                                await dbContext.SaveChangesAsync(stoppingToken);

                                // 2. 📡 실시간 양품 +1 수량 카운트 펄스를 웹 화면으로 전송                                                                                                            
                                await _hubContext.Clients.All.SendAsync("ReceiveSensorCountUpdated", new
                                {
                                    EquipmentID = equipment.EquipmentID,
                                    LotID = lotId,
                                    GoodIncrement = 1,
                                    BadIncrement = 0,
                                    Timestamp = DateTime.UtcNow
                                }, stoppingToken);

                                _logger.LogInformation("⚡ [센서 카운트 +1] 설비: {EqId}, Lot: {LotId}, 누적가동: {Sec}초",
                                    equipment.EquipmentID, lotId, equipment.TotalRunningSeconds);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "⚠️ 센서 백그라운드 서비스 동작 중 예외 발생 (스킵 후 다음 주기 재시도)");
                    }
                }
                                                                                                                   
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task<List<int>> GetAllBomProcessIdsAsync(MESDbContext dbContext, string productId, CancellationToken stoppingToken = default)
        {
            var processIds = new HashSet<int>();
            var queue = new Queue<string>();
            queue.Enqueue(productId);
            var visitedProducts = new HashSet<string> { productId };

            while (queue.Count > 0)
            {
                var currentProduct = queue.Dequeue();
                var boms = await dbContext.BOMs.Where(b => b.ProductID == currentProduct).ToListAsync(stoppingToken);

                foreach (var bom in boms)
                {
                    processIds.Add(bom.ProcessID);
                    if (!visitedProducts.Contains(bom.ChildProductID))
                    {
                        visitedProducts.Add(bom.ChildProductID);
                        queue.Enqueue(bom.ChildProductID);
                    }
                }
            }

            return processIds.ToList();
        }
    }
}

using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.Analytics;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services.OpcService
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<OpcUaBackgroundService> _logger;

        private readonly SemaphoreSlim _lock = new(1, 1);
        private double _latestSinusoid = 0.0;
        private const string DemoEquipmentId = "CNC01";
        private const int DemoProcessId = 2; // CNC 선삭 공정

        public OpcUaBackgroundService(
            IOpcUaService opcUaService,
            IServiceScopeFactory scopeFactory,
            IHubContext<MesHub> hubContext,
            ILogger<OpcUaBackgroundService> logger)
        {
            _opcUaService = opcUaService;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [OPC UA Pulse 수집 서비스] CNC01 실시간 생산 연동 가동");

            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                try
                {
                    switch (tagName)
                    {
                        case "Counter":
                            await HandleCounterPulseAsync(timestamp, stoppingToken);
                            break;

                        case "Sinusoid":
                            if (double.TryParse(value?.ToString(), out double sVal))
                            {
                                _latestSinusoid = sVal;
                                await BroadcastTelemetryAsync(timestamp, stoppingToken);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ CNC01 OPC UA 처리 중 오류 발생");
                }
            };

            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

        private async Task HandleCounterPulseAsync(DateTime timestamp, CancellationToken ct)
        {
            if (!await _lock.WaitAsync(100, ct))
            {
                return; // 이전 펄스 처리 중이면 유실 방지 및 중복 스킵
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                var cnc01 = await dbContext.Equipments.FirstOrDefaultAsync(e => e.EquipmentID == DemoEquipmentId, ct);
                if (cnc01 == null || cnc01.Status != EquipmentStatus.Running || string.IsNullOrEmpty(cnc01.CurrentLotId))
                {
                    return; 
                }

                var cnc01Lot = await dbContext.Lots
                    .Include(l => l.WorkOrder)
                    .FirstOrDefaultAsync(l => l.LotID == cnc01.CurrentLotId, ct);

                if (cnc01Lot == null || cnc01Lot.Status != LotStatus.WIP)
                {
                    return;
                }

                // 3. 작업자 UserID 조회 (FK 보호)
                var validUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserRole == "Operator" || u.UserRole == "Worker", ct)
                    ?? await dbContext.Users.FirstOrDefaultAsync(ct);
                string activeUserId = validUser?.UserID ?? "admin";

                // 4. 현재 LOT 누적 생산량 및 목표 수량 확인
                int currentGoodQty = await dbContext.Performances
                    .Where(p => p.LotID == cnc01Lot.LotID)
                    .SumAsync(p => p.GoodQty, ct);

                int targetQty = (cnc01Lot.WorkOrder?.TargetQty > 0) ? cnc01Lot.WorkOrder.TargetQty : 20;

                // 5. 목표 수량 미만일 때만 실적 등록
                if (currentGoodQty < targetQty)
                {
                    int newGoodQty = currentGoodQty + 1;

                    var perf = new Performance
                    {
                        WorkOrderID = cnc01Lot.OrderID,
                        LotID = cnc01Lot.LotID,
                        ProcessID = cnc01Lot.CurrentProcessID > 0 ? cnc01Lot.CurrentProcessID : DemoProcessId,
                        UserID = activeUserId,
                        InputQty = 1,
                        GoodQty = 1,
                        BadQty = 0,
                        WorkDate = DateTime.Now
                    };
                    dbContext.Performances.Add(perf);

                    if (cnc01Lot.WorkOrder != null)
                    {
                        cnc01Lot.WorkOrder.TotalGoodQty = newGoodQty;
                        if (cnc01Lot.WorkOrder.Status == OrderStatus.Created)
                        {
                            cnc01Lot.WorkOrder.Status = OrderStatus.InProgress;
                        }

                        // 💡 원자재 자동 차감 (BOM 기준)
                        var boms = await dbContext.BOMs
                            .Where(b => b.ProductID == cnc01Lot.WorkOrder.ProductID)
                            .ToListAsync(ct);

                        foreach (var bom in boms)
                        {
                            var rawProduct = await dbContext.ProductMasters
                                .FirstOrDefaultAsync(p => p.ProductID == bom.ChildProductID, ct);

                            if (rawProduct != null)
                            {
                                rawProduct.StockQty = Math.Max(0, rawProduct.StockQty - bom.RequiredQty);
                                await _hubContext.Clients.All.SendAsync("StockUpdated", new
                                {
                                    productID = rawProduct.ProductID,
                                    productName = rawProduct.ProductName,
                                    currentStock = rawProduct.StockQty,
                                    safetyStock = rawProduct.SafetyStock
                                }, ct);
                            }
                        }
                    }

                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var daily = await dbContext.DailyEquipmentProductions
                        .FirstOrDefaultAsync(d => d.EquipmentID == DemoEquipmentId && d.WorkDate == today, ct);

                    int runningMin = (cnc01 != null) ? (int)(cnc01.TotalRunningSeconds / 60) : 0;
                    int downMin = (cnc01 != null) ? (int)(cnc01.TotalDowntimeSeconds / 60) : 0;

                    if (daily == null)
                    {
                        daily = new DailyEquipmentProduction
                        {
                            EquipmentID = DemoEquipmentId,
                            WorkDate = today,
                            PlannedProductionMinutes = 480,
                            OperatingMinutes = Math.Max(1, runningMin),
                            DowntimeMinutes = downMin,
                            TotalProducedQty = 1,
                            GoodQty = 1,
                            DefectQty = 0,
                            IdealCycleTimeMinutes = 0.5m
                        };
                        dbContext.DailyEquipmentProductions.Add(daily);
                    }
                    else
                    {
                        daily.GoodQty += 1;
                        daily.TotalProducedQty += 1;
                        daily.OperatingMinutes = Math.Max(daily.OperatingMinutes, runningMin);
                        daily.DowntimeMinutes = downMin;
                    }

                    _logger.LogInformation("✨ [OPC UA Counter] CNC01 ➔ LOT [{LotId}] 양품 +1 생산 완료! ({Current}/{Target}EA)",
                        cnc01Lot.LotID, newGoodQty, targetQty);

                    if (cnc01Lot.Status == LotStatus.RELEASED)
                    {
                        cnc01Lot.Status = LotStatus.WIP;
                    }

                    if (cnc01 != null)
                    {
                        cnc01.TotalRunningSeconds += 3;
                    }

                    // 목표 수량 도달 시 자동 마감
                    if (newGoodQty >= targetQty)
                    {
                        _logger.LogInformation("🎯 [OPC UA 완료] LOT [{LotId}] 목표 {Target}EA 생산 마감 완료 ➔ CNC01 정지(STOPPED)",
                            cnc01Lot.LotID, targetQty);

                        cnc01Lot.Status = LotStatus.DONE;

                        if (cnc01Lot.WorkOrder != null)
                        {
                            cnc01Lot.WorkOrder.Status = OrderStatus.Completed;
                        }
                    }

                    await dbContext.SaveChangesAsync(ct);

                    // 실시간 SignalR 브로드캐스트
                    await _hubContext.Clients.All.SendAsync("LotUpdated", new { status = cnc01Lot.Status.ToString(), lotId = cnc01Lot.LotID }, ct);
                    await _hubContext.Clients.All.SendAsync("DailyProductionUpdated", ct);
                    await _hubContext.Clients.All.SendAsync("OeeUpdated", ct);

                    if (cnc01Lot.OrderID > 0)
                    {
                        await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new
                        {
                            orderId = cnc01Lot.OrderID,
                            totalGoodQty = newGoodQty,
                            status = cnc01Lot.WorkOrder?.Status.ToString()
                        }, ct);
                    }

                    await _hubContext.Clients.All.SendAsync("ReceiveSensorCountUpdated", new
                    {
                        status = "UPDATED",
                        lotId = cnc01Lot.LotID,
                        goodIncrement = 1,
                        badIncrement = 0,
                        equipmentId = DemoEquipmentId
                    }, ct);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task BroadcastTelemetryAsync(DateTime timestamp, CancellationToken ct)
        {
            if (!await _lock.WaitAsync(50, ct))
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                var today = DateOnly.FromDateTime(DateTime.Today);
                var allEquipments = await dbContext.Equipments.AsNoTracking().ToListAsync(ct);
                var dailyProds = await dbContext.DailyEquipmentProductions.AsNoTracking()
                    .Where(d => d.WorkDate == today)
                    .ToListAsync(ct);

                var telemetryList = new List<object>();

                foreach (var eq in allEquipments)
                {
                    var daily = dailyProds.FirstOrDefault(d => d.EquipmentID == eq.EquipmentID);
                    int eqQty = daily?.GoodQty ?? 0;

                    // 만약 daily 레코드가 아직 없는데 CurrentLotId가 있으면 해당 Lot의 실적 조회
                    if (eqQty == 0 && !string.IsNullOrEmpty(eq.CurrentLotId))
                    {
                        eqQty = await dbContext.Performances
                            .Where(p => p.LotID == eq.CurrentLotId)
                            .SumAsync(p => p.GoodQty, ct);
                    }

                    telemetryList.Add(new
                    {
                        EquipmentId = eq.EquipmentID,
                        Temperature = eq.EquipmentID == DemoEquipmentId
                            ? Math.Round(65.0 + (_latestSinusoid * 15.0), 1)
                            : Math.Round(25.0 + (_latestSinusoid * 1.5), 1),
                        Status = eq.Status,
                        TotalCount = eqQty,
                        Timestamp = timestamp
                    });
                }

                await _hubContext.Clients.All.SendAsync("ReceiveEquipmentTelemetryList", telemetryList, ct);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}

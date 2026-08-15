using mes_server.Hubs;
using mes_server.Models.Analytics;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.MasterData;
using mes_server.Repositories.Interface.Production;
using mes_server.Services.EquipmentService;
using mes_server.Services.Interface;
using mes_server.Services.InventoryService;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Services.ProductionService
{
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IBOMRepository _bomRepository;
        private readonly IGenericRepository<Equipment> _equipmentRepository;
        private readonly IUserRepository _userRepository;

        private readonly IWorkOrderService _workOrderService;
        private readonly IInventoryService _inventoryService;
        private readonly IDailyEquipmentProductionService _dailyEquipmentProductionService;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(
            IPerformanceRepository performanceRepository, 
            ILotRepository lotRepository, 
            IGenericRepository<WorkOrder> workOrderRepository, 
            IWorkOrderService workOrderService, 
            IInventoryService inventoryService, 
            IGenericRepository<ProcessMaster> processMasterRepository, 
            IBOMRepository bomRepository,
            IDailyEquipmentProductionService dailyEquipmentProductionService,
            IGenericRepository<Equipment> equipmentRepository,
            IUserRepository userRepository,
            IHubContext<MesHub> hubContext,
            ILogger<PerformanceService> logger
            )
        {
            _performanceRepository = performanceRepository;
            _lotRepository = lotRepository;
            _workOrderRepository = workOrderRepository;
            _workOrderService = workOrderService;
            _inventoryService = inventoryService;
            _processMasterRepository = processMasterRepository;
            _bomRepository = bomRepository;
            _dailyEquipmentProductionService = dailyEquipmentProductionService;
            _equipmentRepository = equipmentRepository;
            _userRepository = userRepository;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId)
        {
            var perf = await _performanceRepository.GetPerformanceByWorkOrderAsync(orderId);
            return perf;
        }

        public async Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId)
        {
            var perf = new Performance
            {
                WorkOrderID = registerDto.WorkOrderID,
                LotID = registerDto.LotID,
                ProcessID = registerDto.ProcessID,
                ToolID = registerDto.ToolID,
                ReasonCode = registerDto.ReasonCode,
                UserID = userId,
                InputQty = registerDto.InputQty,
                GoodQty = registerDto.GoodQty,
                BadQty = registerDto.BadQty,
                WorkDate = DateTime.Now
            };
            var (lot, workOrder) = await ValidateProductionAsync(registerDto);

            await _performanceRepository.CreateAsync(perf);
            await _inventoryService.ConsumeMaterialByProcessAsync(perf.WorkOrderID, perf.ProcessID, perf.GoodQty);

            workOrder.TotalBadQty += perf.BadQty;

            if (perf.BadQty > 0 && lot != null)
            {
                lot.Status = LotStatus.HOLD;
            }

            var lastProcessId = await GetLastProcessIdForProductAsync(workOrder);

            if (lastProcessId != null && perf.ProcessID == lastProcessId)
            {
                workOrder.TotalGoodQty += perf.GoodQty;
                await _inventoryService.ReceiveFinishedProductAsync(perf.WorkOrderID, perf.GoodQty);

                if (workOrder != null && workOrder.Status != OrderStatus.Completed)
                {
                    if (workOrder.TotalGoodQty >= workOrder.TargetQty)
                    {
                        workOrder.Status = OrderStatus.Completed;
                        await _workOrderService.CompleteWorkOrderAsync(workOrder.OrderID);
                    }
                }
            }

            var targetEquipmentId = perf.ProcessID == 3 ? "CNC03" : (perf.ProcessID == 5 ? "CNC05" : "CNC01");
            await _dailyEquipmentProductionService.CreateDailyEquipmentProductionAsync(targetEquipmentId, DateOnly.FromDateTime(perf.WorkDate), perf.GoodQty, perf.BadQty);

            return perf;
        }

        private async Task<(Lot lot, WorkOrder workOrder)> ValidateProductionAsync(PerformanceRegisterDto registerDto)
        {
            var lot = await _lotRepository.GetByIdAsync(registerDto.LotID);

            if (lot == null)
            {
                throw new ArgumentException($"Lot with ID {registerDto.LotID} does not exist.");
            }

            var workOrder = await _workOrderRepository.GetByIdAsync(registerDto.WorkOrderID);

            if (workOrder == null) {
                throw new ArgumentException($"Work order with ID {registerDto.WorkOrderID} does not exist.");
            }

            return (lot, workOrder);
        }

        private async Task<int?> GetLastProcessIdForProductAsync(WorkOrder workOrder)
        {
            var processList = await _processMasterRepository.GetAllAsync();

            var productProcessIds = await GetAllBomProcessIdsAsync(workOrder.ProductID);

            if (!productProcessIds.Any())
                throw new InvalidOperationException("제품의 BOM 공정을 찾을 수 없습니다.");

            return processList
                .Where(p => productProcessIds.Contains(p.ProcessID))
                .OrderByDescending(p => p.SequenceOrder)
                .Select(p => (int?)p.ProcessID)
                .FirstOrDefault();
        }

        private async Task<List<int>> GetAllBomProcessIdsAsync(string productId)
        {
            var processIds = new HashSet<int>();
            var queue = new Queue<string>();
            queue.Enqueue(productId);
            var visitedProducts = new HashSet<string> { productId };

            while (queue.Count > 0)
            {
                var currentProduct = queue.Dequeue();
                var boms = await _bomRepository.GetAllBomsByProductIdAsync(currentProduct);

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

        public async Task ProcessEquipmentPulseAsync(string equipmentId, DateTime timestamp)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
            if (equipment == null || equipment.Status != EquipmentStatus.Running || string.IsNullOrEmpty(equipment.CurrentLotId))
            {
                return;
            }

            var lot = await _lotRepository.GetLotWithDetailsAsync(equipment.CurrentLotId);
            if (lot == null || (lot.Status != LotStatus.WIP && lot.Status != LotStatus.RELEASED))
            {
                return;
            }

            // 1. 유효한 작업자 UserID 조회
            var users = await _userRepository.GetAllAsync();
            var validUser = users.FirstOrDefault(u => u.UserRole == "Operator" || u.UserRole == "Worker")
                ?? users.FirstOrDefault();
            string activeUserId = validUser?.UserID ?? "admin";

            // 2. 현재 LOT 실적 계산 및 목표 수량 확인
            var performances = await _performanceRepository.GetPerformancesByLotIdAsync(lot.LotID);
            int currentGoodQty = performances.Sum(p => p.GoodQty);
            int targetQty = (lot.WorkOrder?.TargetQty > 0) ? lot.WorkOrder.TargetQty : 20;

            if (currentGoodQty >= targetQty)
            {
                return;
            }

            int newGoodQty = currentGoodQty + 1;
            int processId = lot.CurrentProcessID > 0 ? lot.CurrentProcessID : 2; // CNC 절삭 공정 기본값

            // 3. Performance 등록
            var perf = new Performance
            {
                WorkOrderID = lot.OrderID,
                LotID = lot.LotID,
                ProcessID = processId,
                UserID = activeUserId,
                InputQty = 1,
                GoodQty = 1,
                BadQty = 0,
                WorkDate = DateTime.Now
            };
            await _performanceRepository.CreateAsync(perf);

            // 4. WorkOrder 갱신 및 BOM 자재 차감
            if (lot.WorkOrder != null)
            {
                lot.WorkOrder.TotalGoodQty = newGoodQty;
                if (lot.WorkOrder.Status == OrderStatus.Created)
                {
                    lot.WorkOrder.Status = OrderStatus.InProgress;
                }
                await _inventoryService.ConsumeMaterialByProcessAsync(lot.OrderID, processId, 1);
            }

            // 5. 설비 일일 실적 갱신
            await _dailyEquipmentProductionService.CreateDailyEquipmentProductionAsync(
                equipmentId,
                DateOnly.FromDateTime(DateTime.Today),
                1,
                0
            );

            // 6. 설비 가동시간 누적
            equipment.TotalRunningSeconds += 3;
            await _equipmentRepository.SaveChangesAsync();

            // 7. LOT 상태 갱신 (RELEASED -> WIP)
            if (lot.Status == LotStatus.RELEASED)
            {
                lot.Status = LotStatus.WIP;
            }

            // 8. 목표 수량 도달 시 자동 완료
            if (newGoodQty >= targetQty)
            {
                _logger.LogInformation("🎯 [OPC UA] LOT [{LotId}] 목표 {Target}EA 생산 마감 완료", lot.LotID, targetQty);
                lot.Status = LotStatus.DONE;
                if (lot.WorkOrder != null)
                {
                    lot.WorkOrder.Status = OrderStatus.Completed;
                    await _workOrderService.CompleteWorkOrderAsync(lot.WorkOrder.OrderID);
                }
            }

            await _lotRepository.SaveChangesAsync();
            await _performanceRepository.SaveChangesAsync();

            _logger.LogInformation("✨ [OPC UA Counter] {EquipmentId} ➔ LOT [{LotId}] 양품 +1 생산 완료! ({Current}/{Target}EA)",
                equipmentId, lot.LotID, newGoodQty, targetQty);

            // 9. 실시간 SignalR 브로드캐스트
            await _hubContext.Clients.All.SendAsync("LotUpdated", new { status = lot.Status.ToString(), lotId = lot.LotID });
            await _hubContext.Clients.All.SendAsync("DailyProductionUpdated");
            await _hubContext.Clients.All.SendAsync("OeeUpdated");

            if (lot.OrderID > 0)
            {
                await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new
                {
                    orderId = lot.OrderID,
                    totalGoodQty = newGoodQty,
                    status = lot.WorkOrder?.Status.ToString()
                });
            }

            await _hubContext.Clients.All.SendAsync("ReceiveSensorCountUpdated", new
            {
                status = "UPDATED",
                lotId = lot.LotID,
                goodIncrement = 1,
                badIncrement = 0,
                equipmentId = equipmentId
            });
        }
    }
}

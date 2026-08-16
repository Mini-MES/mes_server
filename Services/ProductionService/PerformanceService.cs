using mes_server.Hubs;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.Production;
using mes_server.Services.EquipmentService;
using mes_server.Services.InventoryService;
using mes_server.Services.MasterDataService;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Services.ProductionService
{
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<Equipment> _equipmentRepository;

        private readonly IWorkOrderService _workOrderService;
        private readonly IInventoryService _inventoryService;
        private readonly IDailyEquipmentProductionService _dailyEquipmentProductionService;
        private readonly IEquipmentService _equipmentService;
        private readonly IMasterDataService _masterDataService;

        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(
            IPerformanceRepository performanceRepository, 
            ILotRepository lotRepository, 
            IGenericRepository<WorkOrder> workOrderRepository, 
            IWorkOrderService workOrderService, 
            IInventoryService inventoryService, 
            IDailyEquipmentProductionService dailyEquipmentProductionService,
            IGenericRepository<Equipment> equipmentRepository,
            IEquipmentService equipmentService,
            IMasterDataService masterDataService,
            IHubContext<MesHub> hubContext,
            ILogger<PerformanceService> logger
            )
        {
            _performanceRepository = performanceRepository;
            _lotRepository = lotRepository;
            _workOrderRepository = workOrderRepository;
            _workOrderService = workOrderService;
            _inventoryService = inventoryService;
            _dailyEquipmentProductionService = dailyEquipmentProductionService;
            _equipmentRepository = equipmentRepository;
            _equipmentService = equipmentService;
            _masterDataService = masterDataService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId)
        {
            var perf = await _performanceRepository.GetPerformanceByWorkOrderAsync(orderId);
            return perf;
        }

        public async Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId, bool autoSave = true, string? equipmentId = null)
        {
            var (lot, workOrder) = await ValidateProductionAsync(registerDto);

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

            await _performanceRepository.CreateAsync(perf);
            await _inventoryService.ConsumeMaterialByProcessAsync(perf.WorkOrderID, perf.ProcessID, perf.GoodQty, autoSave: false);

            workOrder.TotalBadQty += perf.BadQty;

            if (perf.BadQty > 0)
            {
                lot.Status = LotStatus.HOLD;
            }
            else if (lot.Status == LotStatus.RELEASED)
            {
                lot.Status = LotStatus.WIP;
            }

            if (workOrder.Status == OrderStatus.Created)
            {
                workOrder.Status = OrderStatus.InProgress;
            }

            var processes = await _masterDataService.GetOrderedProcessesForProductAsync(workOrder.ProductID);

            var lastProcess = processes.LastOrDefault() ?? throw new InvalidOperationException("제품의 공정 정보가 없습니다.");

            if (perf.ProcessID == lastProcess.ProcessID)
            {
                workOrder.TotalGoodQty += perf.GoodQty;
                await _inventoryService.ReceiveFinishedProductAsync(perf.WorkOrderID, perf.GoodQty, autoSave: false);

                if (workOrder.Status != OrderStatus.Completed && workOrder.TotalGoodQty >= workOrder.TargetQty)
                {
                    await _workOrderService.CompleteWorkOrderAsync(workOrder.OrderID, autoSave: false);
                }
            }

            var targetEquipmentId = equipmentId ?? (perf.ProcessID == 3 ? "CNC03" : (perf.ProcessID == 5 ? "CNC05" : "CNC01")); // TODO : PLC 연결 후 고칠 예정
            await _dailyEquipmentProductionService.CreateDailyEquipmentProductionAsync(
                targetEquipmentId, 
                DateOnly.FromDateTime(perf.WorkDate), 
                perf.GoodQty, 
                perf.BadQty, 
                autoSave: false
            );

            if (autoSave)
            {
                await _performanceRepository.SaveChangesAsync();
            }

            return perf;
        }

        public async Task<Performance?> RecordAutoProductionAsync(string equipmentId, string userId = "OPC_SYSTEM")
        {
            var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
            if (equipment == null || equipment.Status != EquipmentStatus.Running || string.IsNullOrEmpty(equipment.CurrentLotId))
            {
                return null;
            }

            var lot = await _lotRepository.GetLotWithDetailsAsync(equipment.CurrentLotId);
            if (lot == null || (lot.Status != LotStatus.WIP && lot.Status != LotStatus.RELEASED))
            {
                return null;
            }

            var performances = await _performanceRepository.GetPerformancesByLotIdAsync(lot.LotID);
            int currentGoodQty = performances.Sum(p => p.GoodQty);
            int targetQty = (lot.WorkOrder?.TargetQty > 0) ? lot.WorkOrder.TargetQty : 20;

            if (currentGoodQty >= targetQty)
            {
                return null; 
            }

            var registerDto = new PerformanceRegisterDto
            {
                WorkOrderID = lot.OrderID,
                LotID = lot.LotID,
                ProcessID = lot.CurrentProcessID > 0 ? lot.CurrentProcessID : 2,
                GoodQty = 1,
                BadQty = 0,
                InputQty = 1
            };

            var perf = await RegisterPerformanceAsync(registerDto, userId, autoSave: false, equipmentId);

            await _equipmentService.AddRunningTimeAsync(equipmentId, seconds: 3, autoSave: false);
            await _performanceRepository.SaveChangesAsync();

            _logger.LogInformation("✨ [OPC UA Counter] {EquipmentId} ➔ LOT [{LotId}] 자동 실적 등록 완료 ({Current}/{Target}EA)",
                equipmentId, lot.LotID, currentGoodQty + 1, targetQty);

            int updatedGoodQty = currentGoodQty + 1;
            bool isDone = updatedGoodQty >= targetQty;

            await _hubContext.Clients.All.SendAsync("LotUpdated", new
            {
                status = isDone ? LotStatus.DONE.ToString() : LotStatus.WIP.ToString(),
                lotId = lot.LotID
            });

            await _hubContext.Clients.All.SendAsync("DailyProductionUpdated");
            await _hubContext.Clients.All.SendAsync("OeeUpdated");

            if (lot.OrderID > 0)
            {
                await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new
                {
                    orderId = lot.OrderID,
                    totalGoodQty = updatedGoodQty,
                    status = isDone ? OrderStatus.Completed.ToString() : OrderStatus.InProgress.ToString()
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

            return perf;
        }

        private async Task<(Lot lot, WorkOrder workOrder)> ValidateProductionAsync(PerformanceRegisterDto registerDto)
        {
            if (registerDto.InputQty < 0 ||
        registerDto.GoodQty < 0 ||
        registerDto.BadQty < 0)
            {
                throw new ArgumentException(
                    "투입수량, 양품수량, 불량수량은 음수일 수 없습니다.");
            }

            if (registerDto.GoodQty + registerDto.BadQty > registerDto.InputQty)
            {
                throw new ArgumentException(
                    "양품수량과 불량수량의 합은 투입수량을 초과할 수 없습니다.");
            }

            if (registerDto.BadQty > 0 && registerDto.ReasonCode == null)
            {
                throw new ArgumentException(
                    "불량수량이 있으면 불량 사유를 입력해야 합니다.");
            }

            var lot = await _lotRepository.GetByIdAsync(registerDto.LotID)
                ?? throw new KeyNotFoundException(
                    $"LOT ID '{registerDto.LotID}'를 찾을 수 없습니다.");

            var workOrder =
                await _workOrderRepository.GetByIdAsync(registerDto.WorkOrderID)
                ?? throw new KeyNotFoundException(
                    $"생산지시 ID '{registerDto.WorkOrderID}'를 찾을 수 없습니다.");

            if (lot.OrderID != workOrder.OrderID)
            {
                throw new InvalidOperationException(
                    "선택한 LOT이 해당 생산지시에 속하지 않습니다.");
            }

            if (lot.CurrentProcessID != registerDto.ProcessID)
            {
                throw new InvalidOperationException(
                    "입력한 공정이 LOT의 현재 공정과 일치하지 않습니다.");
            }

            if (workOrder.Status == OrderStatus.Completed)
            {
                throw new InvalidOperationException(
                    "완료된 생산지시에는 실적을 등록할 수 없습니다.");
            }

            if (lot.Status != LotStatus.RELEASED &&
                lot.Status != LotStatus.WIP)
            {
                throw new InvalidOperationException(
                    "대기 또는 진행 중인 LOT에만 실적을 등록할 수 있습니다.");
            }

            return (lot, workOrder);
        }
    }
}

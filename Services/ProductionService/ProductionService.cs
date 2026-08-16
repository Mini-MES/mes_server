using mes_server.Data;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.Production;
using mes_server.Services.InventoryService;

namespace mes_server.Services.ProductionService
{
    public class ProductionService : IProductionService
    {
        private readonly IWorkOrderService _workOrderService;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IGenericRepository<Equipment> _equipmentRepository;
        private readonly MESDbContext _context;

        private readonly IInventoryService _inventoryService;
        private readonly IPerformanceService _performanceService;
        private readonly ILotService _lotService;

        public ProductionService(
            IWorkOrderService workOrderService,
            ILotRepository lotRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IGenericRepository<Equipment> equipmentRepository,
            MESDbContext context,
            IInventoryService inventoryService,
            IPerformanceService performanceService,
            ILotService lotService
            )
        {
            _workOrderService = workOrderService;
            _lotRepository = lotRepository;
            _processMasterRepository = processMasterRepository;
            _equipmentRepository = equipmentRepository;
            _inventoryService = inventoryService;
            _performanceService = performanceService;
            _lotService = lotService;
            _context = context;
        }

        public async Task<StartProductionResponseDto> StartProductionAsync(int orderId, StartProductionDto dto)
        {
            var order = await _workOrderService.StartWorkOrderAsync(orderId, autoSave : false);
            if (order == null || order.Status == OrderStatus.Completed)
                throw new InvalidOperationException("진행 불가능한 생산지시입니다.");

            var isAvailable = await _inventoryService.CheckMaterialAvailabilityAsync(order.ProductID, order.TargetQty);
            if (!isAvailable)
            {
                throw new InvalidOperationException($"생산에 필요한 원자재 재고가 부족하여 생산을 시작할 수 없습니다. (계획 수량: {order.TargetQty} EA)");
            }

            var lot = await _lotRepository.GetByIdAsync(dto.lotId) ?? throw new KeyNotFoundException("선택한 LOT을 찾을 수 없습니다.");

            if (lot.OrderID != orderId)
            {
                throw new InvalidOperationException(
                    "선택한 LOT이 해당 생산지시에 속하지 않습니다.");
            }

            if (lot.Status != LotStatus.RELEASED)
            {
                throw new InvalidOperationException(
                    "대기 상태의 LOT만 생산을 시작할 수 있습니다.");
            }

            var equipment = await _equipmentRepository.GetByIdAsync(dto.EquipmentID) ?? throw new KeyNotFoundException("선택한 설비를 찾을 수 없습니다.");

            if (!string.IsNullOrEmpty(equipment.CurrentLotId) && equipment.CurrentLotId != lot.LotID)
            {
                throw new InvalidOperationException(
                    "선택한 설비는 이미 다른 LOT을 작업 중입니다.");
            }

            if (equipment.Status == EquipmentStatus.Maintenance ||
                equipment.Status == EquipmentStatus.Error ||
                equipment.Status == EquipmentStatus.Off)
            {
                throw new InvalidOperationException(
                    "선택한 설비는 현재 생산에 사용할 수 없습니다.");
            }

            lot.Status = LotStatus.WIP;

            equipment.Status = EquipmentStatus.Running;
            equipment.CurrentLotId = lot.LotID;
            equipment.LastStatusChangedAt = DateTime.UtcNow;

            await _equipmentRepository.SaveChangesAsync();

            return new StartProductionResponseDto
            {
                WorkOrderID = order.OrderID,
                LotID = lot.LotID,
                EquipmentID = equipment.EquipmentID
            };
        }

        public async Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _performanceService.RegisterPerformanceAsync(perfDto, userId);
                await IsOrderValid(perfDto.ProcessID, nextProcessId);
                await _lotService.ChangeLotProcessAsync(perfDto.LotID, nextProcessId);
                await transaction.CommitAsync();

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task UnholdLotAsync(string lotId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);
            if (lot == null)
            {
                throw new KeyNotFoundException("존재하지 않는 Lot입니다.");
            }
            if (lot.Status != LotStatus.HOLD)
            {
                throw new InvalidOperationException("보류(HOLD) 상태인 Lot만 보류 해제할 수 있습니다.");
            }

            lot.Status = LotStatus.WIP;
            await _lotRepository.SaveChangesAsync();
        }

        private async Task<bool> IsOrderValid(int currentProcessId, int nextProcessId)
        {
            var currentProc = await _processMasterRepository.GetByIdAsync(currentProcessId);
            var nextProc = await _processMasterRepository.GetByIdAsync(nextProcessId);

            if (currentProc == null || nextProc == null)
            {
                throw new KeyNotFoundException("공정 정보를 찾을 수 없습니다.");
            }
            return nextProc.SequenceOrder > currentProc.SequenceOrder;
        }
    }
}
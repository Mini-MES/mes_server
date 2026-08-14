using mes_server.Data;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.Production;
using mes_server.Repositories.Interface.MasterData;
using Microsoft.EntityFrameworkCore;
using mes_server.Services.InventoryService;

namespace mes_server.Services.ProductionService
{
    public class ProductionService : IProductionService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IWorkOrderService _workOrderService;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IGenericRepository<Equipment> _equipmentRepository;
        private readonly IBOMRepository _bomRepository;
        private readonly MESDbContext _context;

        private readonly IInventoryService _inventoryService;
        private readonly IPerformanceService _performanceService;
        private readonly ILotService _lotService;

        public ProductionService(
            IPerformanceRepository performanceRepository,
            IWorkOrderService workOrderService,
            ILotRepository lotRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IGenericRepository<Equipment> equipmentRepository,
            IBOMRepository bomRepository,
            MESDbContext context,
            IInventoryService inventoryService,
            IPerformanceService performanceService,
            ILotService lotService
            )
        {
            _performanceRepository = performanceRepository;
            _workOrderService = workOrderService;
            _lotRepository = lotRepository;
            _processMasterRepository = processMasterRepository;
            _equipmentRepository = equipmentRepository;
            _bomRepository = bomRepository;
            _inventoryService = inventoryService;
            _performanceService = performanceService;
            _lotService = lotService;
            _context = context;
        }
        

        public async Task<bool> IsOrderValid(int currentProcessId, int nextProcessId)
        {
            var currentProc = await _processMasterRepository.GetByIdAsync(currentProcessId);
            var nextProc = await _processMasterRepository.GetByIdAsync(nextProcessId);

            if (currentProc == null || nextProc == null)
            {
                throw new KeyNotFoundException("공정 정보를 찾을 수 없습니다.");
            }
            return nextProc.SequenceOrder > currentProc.SequenceOrder;
        }

        public async Task<string> StartProductionAsync(int orderId)
        {
            var order = await _workOrderService.GetWorkOrderByIdAsync(orderId);
            if (order == null || order.Status == OrderStatus.Completed)
                throw new InvalidOperationException("진행 불가능한 생산지시입니다.");

            var isAvailable = await _inventoryService.CheckMaterialAvailabilityAsync(order.ProductID, order.TargetQty);
            if (!isAvailable)
            {
                throw new InvalidOperationException($"생산에 필요한 원자재 재고가 부족하여 생산을 시작할 수 없습니다. (계획 수량: {order.TargetQty} EA)");
            }

            var lots = await _lotRepository.GetLotsByOrderIdAsync(orderId);
            var lot = lots.FirstOrDefault();
            if (lot == null)
            {
                throw new KeyNotFoundException("해당 생산지시에 연결된 Lot이 존재하지 않습니다.");
            }

            lot.Status = LotStatus.WIP;
            order.Status = OrderStatus.InProgress;

            var equipment = await _context.Equipments.FirstOrDefaultAsync();
            if (equipment != null)
            {
                equipment.Status = EquipmentStatus.Running;
                equipment.CurrentLotId = lot.LotID;
            }

            await _equipmentRepository.SaveChangesAsync();

            return lot.LotID;
        }
        public async Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _performanceService.RegisterPerformanceAsync(perfDto, userId);
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

        private async Task<List<int>> GetAllBomProcessIdsAsync(string productId)
        {
            var processIds = new HashSet<int>();
            var queue = new Queue<string>();
            queue.Enqueue(productId);
            var visitedProducts = new HashSet<string> { productId };

            while (queue.Count > 0)
            {
                var currentProduct = queue.Dequeue();
                var boms = await _bomRepository.GetAllBomsByProductIdAsync(productId);

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
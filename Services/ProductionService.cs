using mes_server.Data;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.Production;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class ProductionService : IProductionService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IInventoryService _inventoryService;
        private readonly MESDbContext _context;

        public ProductionService(MESDbContext context,
            IPerformanceRepository performanceRepository,
            IWorkOrderRepository workOrderRepository,
            ILotRepository lotRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IInventoryService inventoryService)
        {
            _context = context;
            _performanceRepository = performanceRepository;
            _workOrderRepository = workOrderRepository;
            _lotRepository = lotRepository;
            _processMasterRepository = processMasterRepository;
            _inventoryService = inventoryService;
        }

        public async Task ChangeLotProcessAsync(string lotId, int nextProcessId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);

            if (lot == null)
            {
                throw new KeyNotFoundException("존재하지 않는 Lot입니다.");
            }

            if (!await IsOrderValid(lot.CurrentProcessID, nextProcessId))
            {
                throw new InvalidOperationException("잘못된 공정 순서입니다.");
            }

            lot.CurrentProcessID = nextProcessId;
            await _context.SaveChangesAsync();
        }

        public async Task CompleteWorkOrderAsync(int orderId)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.Completed;
            }
            else
            {
                throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");
            }
        }

        public async Task CreateWorkOrderAsync(WorkOrder workOrder)
        {
            bool isAvailable = await _inventoryService.CheckMaterialAvailabilityAsync(workOrder.ProductID, workOrder.TargetQty);
            if (!isAvailable)
            {
                throw new InvalidOperationException("재고가 부족합니다.");
            }

            workOrder.Status = OrderStatus.Created;

            await _workOrderRepository.CreateAsync(workOrder);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId)
        {
            var perf = await _performanceRepository.GetPerformanceByWorkOrderAsync(orderId);
            return perf;
        }

        public async Task RegisterPerformanceAsync(Performance perf)
        {


            var lot = await _lotRepository.GetByIdAsync(perf.LotID);
            if (lot == null) throw new KeyNotFoundException("존재하지 않는 Lot입니다.");

            var workOrder = await _workOrderRepository.GetByIdAsync(perf.WorkOrderID);
            if (workOrder == null) throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");

            await _performanceRepository.CreateAsync(perf);
            await _inventoryService.ConsumeMaterialByProcessAsync(perf.WorkOrderID, perf.ProcessID, perf.GoodQty);

            workOrder.TotalBadQty += perf.BadQty;
            workOrder.TotalGoodQty += perf.GoodQty;

            if (perf.BadQty > 0 && lot != null)
            {
                lot.Status = LotStatus.HOLD;
            }

            var processList = await _processMasterRepository.GetAllAsync();
            var lastProcess = processList.OrderByDescending(p => p.SequenceOrder).FirstOrDefault()?.ProcessID;
            if (lastProcess != null && perf.ProcessID == lastProcess)
            {
                await _inventoryService.ReceiveFinishedProductAsync(perf.WorkOrderID, perf.GoodQty);
            }


            if (workOrder != null && workOrder.Status != OrderStatus.Completed)
            {
                if (workOrder.TotalGoodQty >= workOrder.TargetQty)
                {
                    workOrder.Status = OrderStatus.Completed;
                    await CompleteWorkOrderAsync(workOrder.OrderID);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsOrderValid(int currentProcessId, int nextProcessId)
        {
            var currentProc = await _processMasterRepository.GetByIdAsync(currentProcessId);
            var nextProc = await _processMasterRepository.GetByIdAsync(nextProcessId);

            if (currentProc == null || nextProc == null)
            {
                throw new KeyNotFoundException("공정 정보를 찾을 수 없습니다.");
            }
            return nextProc.SequenceOrder == currentProc.SequenceOrder + 1; // 다음 공정인지 체크
        }

        public async Task<string> StartProductionAsync(int orderId, string lotId)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status == OrderStatus.Completed)
                throw new InvalidOperationException("진행 불가능한 생산지시입니다.");

            var newLot = new Lot { LotID = lotId, OrderID = orderId, CurrentProcessID = 1, Status = LotStatus.WIP };
            await _lotRepository.CreateAsync(newLot);

            order.Status = OrderStatus.InProgress;
            await _context.SaveChangesAsync();

            return lotId;
        }
        public async Task MoveProcessAsync(Performance perf, int nextProcessId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await RegisterPerformanceAsync(perf);
                await ChangeLotProcessAsync(perf.LotID, nextProcessId);
                await transaction.CommitAsync();

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }
    }
}
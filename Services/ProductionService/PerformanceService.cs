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


namespace mes_server.Services.ProductionService
{
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IBOMRepository _bomRepository;

        private readonly IWorkOrderService _workOrderService;
        private readonly IInventoryService _inventoryService;
        private readonly IDailyEquipmentProductionService _dailyEquipmentProductionService;



        public PerformanceService(
            IPerformanceRepository performanceRepository, 
            ILotRepository lotRepository, 
            IGenericRepository<WorkOrder> workOrderRepository, 
            IWorkOrderService workOrderService, 
            IInventoryService inventoryService, 
            IGenericRepository<ProcessMaster> processMasterRepository, 
            IBOMRepository bomRepository,
            IDailyEquipmentProductionService dailyEquipmentProductionService
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
    }
}

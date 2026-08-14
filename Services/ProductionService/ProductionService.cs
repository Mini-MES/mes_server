using mes_server.Data;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.History;
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
        private readonly IBOMRepository _bomRepository;
        private readonly IInventoryService _inventoryService;
        private readonly MESDbContext _context;

        public ProductionService(
            IPerformanceRepository performanceRepository,
            IWorkOrderService workOrderService,
            ILotRepository lotRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IBOMRepository bomRepository,
            IInventoryService inventoryService,
            MESDbContext context)
        {
            _performanceRepository = performanceRepository;
            _workOrderService = workOrderService;
            _lotRepository = lotRepository;
            _processMasterRepository = processMasterRepository;
            _bomRepository = bomRepository;
            _inventoryService = inventoryService;
            _context = context;
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

            var lot = await _lotRepository.GetByIdAsync(perf.LotID);
            if (lot == null) throw new KeyNotFoundException("존재하지 않는 Lot입니다.");

            var workOrder = await _workOrderService.GetWorkOrderByIdAsync(perf.WorkOrderID);
            if (workOrder == null) throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");

            await _performanceRepository.CreateAsync(perf);
            await _inventoryService.ConsumeMaterialByProcessAsync(perf.WorkOrderID, perf.ProcessID, perf.GoodQty);

            workOrder.TotalBadQty += perf.BadQty;

            if (perf.BadQty > 0 && lot != null)
            {
                lot.Status = LotStatus.HOLD;
            }

            var processList = await _processMasterRepository.GetAllAsync();
            var productBoms = await _bomRepository.FindAsync(b => b.ProductID == workOrder.ProductID);
            var productProcessIds = productBoms.Select(b => b.ProcessID).Distinct().ToList();

            int? lastProcessId = null;
            if (productProcessIds.Any())
            {
                var productProcesses = processList.Where(p => productProcessIds.Contains(p.ProcessID));
                lastProcessId = productProcesses.OrderByDescending(p => p.SequenceOrder).FirstOrDefault()?.ProcessID;
            }

            if (lastProcessId == null)
            {
                lastProcessId = processList.OrderByDescending(p => p.SequenceOrder).FirstOrDefault()?.ProcessID;
            }
            
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
            var today = DateOnly.FromDateTime(DateTime.Today);
            var daily = await _context.DailyEquipmentProductions
                .FirstOrDefaultAsync(d => d.EquipmentID == targetEquipmentId && d.WorkDate == today);

            var eq = await _context.Equipments.FirstOrDefaultAsync(e => e.EquipmentID == targetEquipmentId);
            int runningMin = (eq != null) ? (int)(eq.TotalRunningSeconds / 60) : 0;
            int downMin = (eq != null) ? (int)(eq.TotalDowntimeSeconds / 60) : 0;

            if (daily == null)
            {
                daily = new mes_server.Models.Analytics.DailyEquipmentProduction
                {
                    EquipmentID = targetEquipmentId,
                    WorkDate = today,
                    PlannedProductionMinutes = 480,
                    OperatingMinutes = Math.Max(1, runningMin),
                    DowntimeMinutes = downMin,
                    TotalProducedQty = perf.GoodQty + perf.BadQty,
                    GoodQty = perf.GoodQty,
                    DefectQty = perf.BadQty,
                    IdealCycleTimeMinutes = 0.5m
                };
                _context.DailyEquipmentProductions.Add(daily);
            }
            else
            {
                daily.GoodQty += perf.GoodQty;
                daily.DefectQty += perf.BadQty;
                daily.TotalProducedQty += (perf.GoodQty + perf.BadQty);
                daily.OperatingMinutes = Math.Max(daily.OperatingMinutes, runningMin);
                daily.DowntimeMinutes = downMin;
            }

            await _context.SaveChangesAsync();

            return perf;
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

            var lots = await _lotRepository.FindAsync(l => l.OrderID == orderId);
            var lot = lots.FirstOrDefault();
            if (lot == null)
            {
                throw new KeyNotFoundException("해당 생산지시에 연결된 Lot이 존재하지 않습니다.");
            }

            lot.Status = LotStatus.WIP;
            order.Status = OrderStatus.InProgress;

            // 💡 생산 지시 시작 시 설비에 해당 LotID를 즉시 바인딩하여 센서 서비스가 올바른 Lot으로 펄스를 발행하도록 설정
            var equipment = await _context.Equipments.FirstOrDefaultAsync();
            if (equipment != null)
            {
                equipment.Status = EquipmentStatus.Running;
                equipment.CurrentLotId = lot.LotID;
            }

            await _context.SaveChangesAsync();

            return lot.LotID;
        }
        public async Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await RegisterPerformanceAsync(perfDto, userId);
                // await ChangeLotProcessAsync(perfDto.LotID, nextProcessId);
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
            await _context.SaveChangesAsync();
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
                var boms = await _bomRepository.FindAsync(b => b.ProductID == currentProduct);

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
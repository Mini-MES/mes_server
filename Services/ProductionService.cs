using mes_server.Data;
using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.Production;
using mes_server.Repositories.Interface.MasterData;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class ProductionService : IProductionService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IBOMRepository _bomRepository;
        private readonly IInventoryService _inventoryService;
        private readonly MESDbContext _context;

        public ProductionService(MESDbContext context,
            IPerformanceRepository performanceRepository,
            IWorkOrderRepository workOrderRepository,
            ILotRepository lotRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IBOMRepository bomRepository,
            IInventoryService inventoryService)
        {
            _context = context;
            _performanceRepository = performanceRepository;
            _workOrderRepository = workOrderRepository;
            _lotRepository = lotRepository;
            _processMasterRepository = processMasterRepository;
            _bomRepository = bomRepository;
            _inventoryService = inventoryService;
        }

        public async Task ChangeLotProcessAsync(string lotId, int nextProcessId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);

            if (lot == null)
            {
                throw new KeyNotFoundException("존재하지 않는 Lot입니다.");
            }

            if (lot.Status == LotStatus.HOLD)
            {
                throw new InvalidOperationException("보류(HOLD) 상태의 Lot은 공정을 이동할 수 없습니다. 보류 해제 또는 재작업 처리가 필요합니다.");
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
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");
            }
        }

        private string GenerateLotId()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringPart = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            var numberPart = random.Next(100).ToString("D2");
            return stringPart + numberPart;
        }

        private async Task<string> GenerateUniqueLotIdAsync()
        {
            string lotId;
            do
            {
                lotId = GenerateLotId();
            } while (await _lotRepository.GetByIdAsync(lotId) != null);
            return lotId;
        }

        public async Task<WorkOrderResponseDto> CreateWorkOrderAsync(WorkOrderCreateDto createDto)
        {
            var workOrder = new WorkOrder
            {
                ProductID = createDto.ProductID,
                TargetQty = createDto.TargetQty,
                StartDate = createDto.StartDate,
                DueDate = createDto.DueDate
            };

            workOrder.Status = OrderStatus.Created;

            await _workOrderRepository.CreateAsync(workOrder);
            await _context.SaveChangesAsync();

            var processList = await _processMasterRepository.GetAllAsync();
            var firstProcess = processList.OrderBy(p => p.SequenceOrder).FirstOrDefault()?.ProcessID;
            if (firstProcess == null)
            {
                throw new InvalidOperationException("등록된 공정이 존재하지 않아 Lot을 자동 생성할 수 없습니다.");
            }

            var lotId = await GenerateUniqueLotIdAsync();
            var newLot = new Lot
            {
                LotID = lotId,
                OrderID = workOrder.OrderID,
                CurrentProcessID = firstProcess.Value,
                Status = LotStatus.RELEASED
            };
            await _lotRepository.CreateAsync(newLot);
            await _context.SaveChangesAsync();

            return new WorkOrderResponseDto
            {
                OrderID = workOrder.OrderID,
                ProductID = workOrder.ProductID,
                TargetQty = workOrder.TargetQty,
                TotalGoodQty = workOrder.TotalGoodQty,
                TotalBadQty = workOrder.TotalBadQty,
                Status = workOrder.Status,
                OrderDate = workOrder.OrderDate,
                StartDate = workOrder.StartDate,
                DueDate = workOrder.DueDate,
                LotID = lotId
            };
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

            var workOrder = await _workOrderRepository.GetByIdAsync(perf.WorkOrderID);
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
                        await CompleteWorkOrderAsync(workOrder.OrderID);
                    }
                }
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
            return nextProc.SequenceOrder == currentProc.SequenceOrder + 1; // 다음 공정인지 체크
        }

        public async Task<string> StartProductionAsync(int orderId)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
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
            await _context.SaveChangesAsync();

            return lot.LotID;
        }
        public async Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await RegisterPerformanceAsync(perfDto, userId);
                await ChangeLotProcessAsync(perfDto.LotID, nextProcessId);
                await transaction.CommitAsync();

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task UpdateWorkOrderAsync(int orderId, WorkOrderUpdateDto updateDto)
        {
            var existingOrder = await _workOrderRepository.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");
            }
            if (existingOrder.Status == OrderStatus.InProgress || existingOrder.Status == OrderStatus.Completed)
            {
                throw new InvalidOperationException("이미 진행 중이거나 완료된 생산 지시는 수정할 수 없습니다.");
            }
            existingOrder.TargetQty = updateDto.TargetQty;
            existingOrder.StartDate = updateDto.StartDate;
            existingOrder.DueDate = updateDto.DueDate;
            existingOrder.Status = updateDto.Status;

            await _context.SaveChangesAsync();
        }
        public async Task DeleteWorkOrderAsync(int orderId)
        {
            var existingOrder = await _workOrderRepository.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");
            }

            if (existingOrder.Status == OrderStatus.InProgress || existingOrder.Status == OrderStatus.Completed)
            {
                throw new InvalidOperationException("이미 진행 중이거나 완료된 생산 지시는 삭제할 수 없습니다.");
            }

            var lots = await _lotRepository.FindAsync(l => l.OrderID == orderId);
            foreach (var lot in lots)
            {
                await _lotRepository.DeleteAsync(lot);
            }

            await _workOrderRepository.DeleteAsync(existingOrder);
            await _context.SaveChangesAsync();
        }

        public async Task<Lot> GetLotStatusAsync(string lotId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);

            if (lot == null)
            {
                throw new KeyNotFoundException($"LOT ID: {lotId}를 찾을 수 없습니다.");
            }

            return lot;
        }

        public async Task<WorkOrderResponseDto?> GetWorkOrderByIdAsync(int orderId)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
            if (order == null) return null;

            var lots = await _lotRepository.FindAsync(l => l.OrderID == orderId);
            var lotId = lots.FirstOrDefault()?.LotID;

            return new WorkOrderResponseDto
            {
                OrderID = order.OrderID,
                ProductID = order.ProductID,
                TargetQty = order.TargetQty,
                TotalGoodQty = order.TotalGoodQty,
                TotalBadQty = order.TotalBadQty,
                Status = order.Status,
                OrderDate = order.OrderDate,
                StartDate = order.StartDate,
                DueDate = order.DueDate,
                LotID = lotId
            };
        }
    }
}
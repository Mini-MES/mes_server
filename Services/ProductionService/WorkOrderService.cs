using mes_server.Models.DTOs.Production;
using mes_server.Models.Enum;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Production;
using mes_server.Services.MasterDataService;

namespace mes_server.Services.ProductionService
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ILotRepository _lotRepository;
        private readonly IMasterDataService _masterDataService;
        private readonly ILotService _lotService;

        public WorkOrderService(IWorkOrderRepository workOrderRepository, ILotRepository lotRepository, IMasterDataService masterDataService, ILotService lotService)
        {
            _workOrderRepository = workOrderRepository;
            _lotRepository = lotRepository;
            _masterDataService = masterDataService;
            _lotService = lotService;
        }



        public async Task CompleteWorkOrderAsync(int orderId, bool autoSave = true)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                if (order.TotalGoodQty < order.TargetQty)
                {
                    throw new InvalidOperationException($"목표 생산 수량({order.TargetQty} EA) 미달 건은 생산 완료 처리할 수 없습니다. (현재: {order.TotalGoodQty} EA)");
                }

                var lots = await _lotRepository.GetLotsByOrderIdAsync(orderId);
                foreach (var lot in lots)
                {
                    if (lot.Status == LotStatus.HOLD)
                    {
                        throw new InvalidOperationException($"LOT ID ({lot.LotID})가 보류(HOLD) 상태입니다. 불량 보류 처리 해제 후 최종 마감할 수 있습니다.");
                    }
                    lot.Status = LotStatus.DONE;
                }

                order.Status = OrderStatus.Completed;
                await _workOrderRepository.UpdateAsync(order);

                if (autoSave)
                {
                    await _workOrderRepository.SaveChangesAsync();
                }
            }
            else
            {
                throw new KeyNotFoundException("존재하지 않는 생산지시입니다.");
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

            await _workOrderRepository.SaveChangesAsync();
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

            var lots = await _lotRepository.GetLotsByOrderIdAsync(orderId);
            foreach (var lot in lots)
            {
                await _lotRepository.DeleteAsync(lot);
            }

            await _workOrderRepository.DeleteAsync(existingOrder);
            await _workOrderRepository.SaveChangesAsync();
        }


        public async Task<WorkOrderResponseDto?> GetWorkOrderByIdAsync(int orderId)
        {
            var order = await _workOrderRepository.GetByIdAsync(orderId);
            if (order == null) return null;

            var lots = await _lotRepository.GetLotsByOrderIdAsync(orderId);
            var lotIds = lots.Select(l => l.LotID).ToList();

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
                LotID = lotIds
            };
        }

        public async Task<WorkOrderResponseDto> CreateWorkOrderAsync(WorkOrderCreateDto createDto)
        {
            if (createDto.TargetQty <= 0)
            {
                throw new ArgumentException("목표 수량은 1 이상이어야 합니다.");
            }

            if (createDto.DueDate < createDto.StartDate)
            {
                throw new ArgumentException("완료 예정일은 시작일보다 빠를 수 없습니다.");
            }

            var processes = await _masterDataService.GetOrderedProcessesForProductAsync(createDto.ProductID);

            var firstProcess = processes.FirstOrDefault() ?? throw new InvalidOperationException("등록된 공정이 존재하지 않아 Lot을 자동 생성할 수 없습니다.");
            var lotId = await _lotService.GenerateUniqueLotIdAsync();

            var workOrder = new WorkOrder
            {
                ProductID = createDto.ProductID,
                TargetQty = createDto.TargetQty,
                StartDate = createDto.StartDate,
                DueDate = createDto.DueDate,
                Status = OrderStatus.Created,
            };

            var newLot = new Lot
            {
                LotID = lotId,
                CurrentProcessID = firstProcess.ProcessID,
                Status = LotStatus.RELEASED
            };

            workOrder.Lots.Add(newLot);

            await _workOrderRepository.CreateAsync(workOrder);
            await _workOrderRepository.SaveChangesAsync();

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
                LotID = new List<string> { lotId }
            };
        }

        public async Task<IEnumerable<WorkOrderResponseDto>> GetAllWorkOrdersAsync()
        {
            var orders = await _workOrderRepository.GetAllWithDetailsAsync();

            return orders.Select(order => new WorkOrderResponseDto
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
                LotID = order.Lots.Select(l => l.LotID).ToList()
            }).ToList();
        }
    }
}

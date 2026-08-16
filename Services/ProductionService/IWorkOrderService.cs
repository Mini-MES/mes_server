using mes_server.Models.DTOs.Production;
using mes_server.Models.Production;

namespace mes_server.Services.ProductionService
{
    public interface IWorkOrderService
    {
        Task<IEnumerable<WorkOrderResponseDto>> GetAllWorkOrdersAsync();
        Task CompleteWorkOrderAsync(int orderId, bool autoSave = true);
        Task<WorkOrderResponseDto> CreateWorkOrderAsync(WorkOrderCreateDto createDto);
        Task UpdateWorkOrderAsync(int orderId, WorkOrderUpdateDto updateDto);
        Task DeleteWorkOrderAsync(int orderId);
        Task<WorkOrderResponseDto?> GetWorkOrderByIdAsync(int orderId);
        Task<WorkOrder> StartWorkOrderAsync(int orderId, bool autoSave = true);
    }
}

using mes_server.Models.DTOs.Production;
using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.Interface
{
    public interface IProductionService
    {
        Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto);
        Task<WorkOrder> CreateWorkOrderAsync(WorkOrderCreateDto createDto);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
        Task ChangeLotProcessAsync(string lotId, int nextProcessId);
        Task CompleteWorkOrderAsync(int orderId);
        Task<string> StartProductionAsync(int orderId, string lotId);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId);
        Task UpdateWorkOrderAsync(int orderId, WorkOrderUpdateDto updateDto);
        Task DeleteWorkOrderAsync(int orderId);
        Task<Lot> GetLotStatusAsync(string lotId);
    }
}

using mes_server.Models.DTOs.Production;
using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.Interface
{
    public interface IProductionService
    {
        Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId);
        Task<WorkOrderResponseDto> CreateWorkOrderAsync(WorkOrderCreateDto createDto);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
        Task ChangeLotProcessAsync(string lotId, int nextProcessId);
        Task CompleteWorkOrderAsync(int orderId);
        Task<string> StartProductionAsync(int orderId);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId);
        Task UpdateWorkOrderAsync(int orderId, WorkOrderUpdateDto updateDto);
        Task DeleteWorkOrderAsync(int orderId);
        Task<Lot> GetLotStatusAsync(string lotId);
        Task<WorkOrderResponseDto?> GetWorkOrderByIdAsync(int orderId);
    }
}

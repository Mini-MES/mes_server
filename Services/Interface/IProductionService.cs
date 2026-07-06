using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.Interface
{
    public interface IProductionService
    {
        Task RegisterPerformanceAsync(Performance perf);
        Task CreateWorkOrderAsync(WorkOrder workOrder);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
        Task ChangeLotProcessAsync(string lotId, int nextProcessId);
        Task CompleteWorkOrderAsync(int orderId);
        Task<string> StartProductionAsync(int orderId, string lotId);
        Task MoveProcessAsync(Performance perf, int nextProcessId);
    }
}

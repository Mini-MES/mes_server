using mes_server.Models.DTOs.Production;

namespace mes_server.Services.ProductionService
{
    public interface IProductionService
    {
        Task<string> StartProductionAsync(int orderId);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId);
        Task UnholdLotAsync(string lotId);
    }
}

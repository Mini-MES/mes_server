using mes_server.Models.DTOs.Production;
using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.ProductionService
{
    public interface IProductionService
    {
        Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
        Task<string> StartProductionAsync(int orderId);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId);
        Task UnholdLotAsync(string lotId);
    }
}

using mes_server.Models.DTOs.Production;
using mes_server.Models.History;

namespace mes_server.Services.ProductionService
{
    public interface IPerformanceService
    {
        Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
        Task ProcessEquipmentPulseAsync(string equipmentId, DateTime timestamp);
    }
}

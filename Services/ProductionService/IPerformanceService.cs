using mes_server.Models.DTOs.Production;
using mes_server.Models.History;

namespace mes_server.Services.ProductionService
{
    public interface IPerformanceService
    {
        Task<Performance> RegisterPerformanceAsync(PerformanceRegisterDto registerDto, string userId, bool autoSave = true, string? equipmentId = null);
        Task<IEnumerable<Performance>> GetProductionStatusAsync(int orderId);
<<<<<<< Updated upstream
        Task<Performance?> RecordAutoProductionAsync(string equipmentId, string userId = "OPC_SYSTEM");
=======
        Task<Performance?> RecordAutoProductionAsync(string equipmentId, int productionQty);
>>>>>>> Stashed changes
    }
}

using mes_server.Models.DTOs.Production;

namespace mes_server.Services.ProductionService
{
    public interface IProductionService
    {
        Task<StartProductionResponseDto> StartProductionAsync(int orderId, StartProductionDto dto, string userId);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId, string? nextEquipmentId = null);
        Task UnholdLotAsync(string lotId);
    }
}

using mes_server.Models.DTOs.Production;

namespace mes_server.Services.ProductionService
{
    public interface IProductionService
    {
        Task<StartProductionResponseDto> StartProductionAsync(int orderId, StartProductionDto dto);
        Task MoveProcessAsync(PerformanceRegisterDto perfDto, int nextProcessId, string userId);
        Task UnholdLotAsync(string lotId);
    }
}

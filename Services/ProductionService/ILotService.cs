using mes_server.Models.Production;

namespace mes_server.Services.ProductionService
{
    public interface ILotService
    {
        Task<string> GenerateUniqueLotIdAsync();
        Task ChangeLotProcessAsync(string lotId, int nextProcessId);
        Task<Lot> GetLotByIdAsync(string lotId);
        Task<IEnumerable<Lot>> GetAllLotsAsync();
    }
}

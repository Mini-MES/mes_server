using mes_server.Models.Enum;
using mes_server.Models.MasterData;

namespace mes_server.Services.Interface
{
    public interface IMasterDataService
    {
        Task<IEnumerable<BOM>> GetProductBOMAsync(string productId);
        Task<IEnumerable<ProcessMaster>> GetProcessListAsync();
        Task<IEnumerable<BadReasonMaster>> GetBadReasonListAsync();
        Task<IEnumerable<BadReasonMaster>> GetBadReasonByCodeAsync(ReasonCode code);
        Task<bool> ValidateBOMAsync(string productId);
        Task<ProcessMaster?> GetProcessBySequenceAsync(int sequence);
    }
}

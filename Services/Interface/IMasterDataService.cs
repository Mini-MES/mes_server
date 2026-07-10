using mes_server.Models.DTOs.MasterData;
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
        Task<ProcessMaster> CreateProcessAsync(ProcessCreateDto dto);
        Task<BadReasonMaster> CreateBadReasonAsync(BadReasonCreateDto dto);
        Task<BOM> AddBomAsync(BOMCreateDto dto);
        Task<ProcessMaster> UpdateProcessAsync(int id, ProcessUpdateDto dto);
        Task<ProductMaster> CreateProductAsync(ProductCreateDto dto);
        Task<ProductMaster> UpdateProductAsync(string productId, ProductUpdateDto dto);
        Task<bool> DeleteBomAsync(string productId, string materialId, int processId);
    }
}

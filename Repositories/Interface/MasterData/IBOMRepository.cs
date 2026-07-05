using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.MasterData
{
    public interface IBOMRepository : IGenericRepository<BOM>
    {
        Task<IEnumerable<BOM>> GetBOMByProductIdAsync(string productId);
    }
}

using mes_server.Data;
using mes_server.Models.MasterData;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.MasterData;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.MasterData
{
    public class BOMRepository : GenericRepository<BOM>, IBOMRepository
    {
        public BOMRepository(MESDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<BOM>> GetBOMByProductIdAsync(string productId)
        {
            return await Context.BOMs
                .Where(b => b.ProductID == productId)
                .Include(b => b.Process)
                .ToListAsync();
        }
    }
}

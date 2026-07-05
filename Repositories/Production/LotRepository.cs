using mes_server.Data;
using mes_server.Models.Production;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.Production;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.Production
{
    public class LotRepository : GenericRepository<Lot>, ILotRepository
    {
        public LotRepository(MESDbContext context) : base(context)
        {
        }

        public async Task<Lot?> GetLotWithDetailsAsync(string lotId)
        {
            return await Context.Lots
                .Include(l => l.WorkOrder)
                .Include(l => l.CurrentProcess)
                .FirstOrDefaultAsync(l => l.LotID == lotId);
        }
    }
}

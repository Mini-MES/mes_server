using mes_server.Data;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.MasterData;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.MasterData
{
    public class BadReasonMasterRepository : GenericRepository<BadReasonMaster>, IBadReasonMasterRepository
    {
        public BadReasonMasterRepository(MESDbContext context) : base(context) { }

        public async Task<IEnumerable<BadReasonMaster>> GetAllBadReasonMasterByCodeAsync(ReasonCode code)
        {
            return await Context.BadReasonMasters
                .Where(b => b.ReasonCode == code)
                .ToListAsync();
        }
    }
}

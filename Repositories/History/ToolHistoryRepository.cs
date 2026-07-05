using mes_server.Data;
using mes_server.Models.History;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.History;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.History
{
    public class ToolRepository : GenericRepository<ToolHistory>, IToolHistoryRepository
    {
        public ToolRepository(MESDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ToolHistory>> GetHistoryByToolIdAsync(string toolId)
        {
            return await Context.ToolHistories
                .Where(th => th.ToolID == toolId)
                .Include(t => t.Tool)
                .OrderByDescending(th => th.ChangeDate)
                .ToListAsync();
        }
    }
}

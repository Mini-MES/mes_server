using mes_server.Data;
using mes_server.Models.History;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.History;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.History
{
    public class PerformanceRepository : GenericRepository<Performance>, IPerformanceRepository
    {
        public PerformanceRepository(MESDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Performance>> GetPerformanceByDateAsync(DateTime startDate, DateTime endDate)
        {
            return await Context.Performances
                .Where(p => p.WorkDate >= startDate && p.WorkDate <= endDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Performance>> GetPerformanceByProcessIdAsync(int processId)
        {
            return await Context.Performances
                .Where(p => p.ProcessID == processId)
                .ToListAsync();
        }

        public async Task<Performance?> GetPerformanceDetailAsync(int perfId)
        {
            return await Context.Performances
                .Where(p => p.PerfID == perfId)
                .Include(p => p.WorkOrder)
                .Include(p => p.Lot)
                .Include(p => p.Tool)
                .Include(p => p.User)
                .Include(p => p.BadReason)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Performance>> GetPerformancesByLotIdAsync(string lotId)
        {
            return await Context.Performances
                .Where(p => p.LotID == lotId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Performance>> GetByWorkOrderAsync(int workOrderId)
        {
            return await Context.Performances
                .Where(p => p.WorkOrderID == workOrderId)
                .ToListAsync();
        }

        public async Task<int> GetTotalGoodQtyByOrderIdAsync(int orderId)
        {
            return await Context.Performances
                .Where(p => p.WorkOrderID == orderId)
                .SumAsync(p => p.GoodQty);
        }
    }
}

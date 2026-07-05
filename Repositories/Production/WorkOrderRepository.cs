using mes_server.Data;
using mes_server.Models.Production;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.Production;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.Production
{
    public class WorkOrderRepository : GenericRepository<WorkOrder>, IWorkOrderRepository
    {
        public WorkOrderRepository(MESDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<WorkOrder>> GetAllWithDetailsAsync()
        {
            return await _context.WorkOrders
                .Include(wo => wo.Product)
                .ToListAsync();
    
        }

        public async Task<WorkOrder?> GetWorkOrderDetailAsync(int workOrderId)
        {
            return await _context.WorkOrders
                .Include(wo => wo.Product)
                .Include(wo => wo.Lots)
                .FirstOrDefaultAsync(wo => wo.OrderID == workOrderId);
        }
    }
}

using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.Production
{
    public interface IWorkOrderRepository : IGenericRepository<WorkOrder> 
    {
        Task<WorkOrder?> GetWorkOrderDetailAsync(int workOrderId);

        Task<IEnumerable<WorkOrder>> GetAllWithDetailsAsync();
    }
}

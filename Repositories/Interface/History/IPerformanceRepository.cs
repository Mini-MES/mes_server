using mes_server.Models.History;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.History
{
    public interface IPerformanceRepository : IGenericRepository<Performance>
    {
        Task<Performance?> GetPerformanceDetailAsync(int perfId);
                
        Task<IEnumerable<Performance>> GetPerformancesByLotIdAsync(string lotId);

        Task<IEnumerable<Performance>> GetPerformanceByProcessIdAsync(int processId);

        Task<IEnumerable<Performance>> GetPerformanceByDateAsync(DateTime startDate, DateTime endDate);
    }
}

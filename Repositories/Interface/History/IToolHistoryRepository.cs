using mes_server.Models.History;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.History
{
    public interface IToolHistoryRepository : IGenericRepository<ToolHistory>
    {
        Task<IEnumerable<ToolHistory>> GetHistoryByToolIdAsync(string toolId);
    }
}

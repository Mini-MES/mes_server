using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.Interface
{
    public interface IToolService
    {
        Task UseToolAsync(string toolId, int usageAmount);
        Task<bool> CheckToolMaintenance(string toolId);
        Task<IEnumerable<ToolHistory>> GetToolHistoryAsync(string toolId);
        Task ResetToolCountAsync(string toolId);
        Task RegisterToolAsync(Tool tool);
    }
}

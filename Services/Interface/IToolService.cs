using mes_server.Models.DTOs.Tool;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.Production;

namespace mes_server.Services.Interface
{
    public interface IToolService
    {
        Task ChangeToolStatusAsync(string toolId, ToolStatus newStatus, string reason);
        Task UseToolAsync(string toolId, int usageAmount);
        Task<bool> CheckToolMaintenance(string toolId);
        Task<IEnumerable<ToolHistory>> GetToolHistoryAsync(string toolId);
        Task ResetToolCountAsync(string toolId);
        Task<Tool> RegisterToolAsync(CreateToolDto dto);
    }
}

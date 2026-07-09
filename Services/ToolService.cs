using mes_server.Data;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class ToolService : IToolService
    {
        private readonly IGenericRepository<Tool> _toolRepository;
        private readonly IToolHistoryRepository _toolHistoryRepository;
        private readonly MESDbContext _context;

        public ToolService(
            MESDbContext context,
            IGenericRepository<Tool> toolRepository,
            IToolHistoryRepository toolHistoryRepository)
        {
            _context = context;
            _toolRepository = toolRepository;
            _toolHistoryRepository = toolHistoryRepository;
        }

        public async Task ChangeToolStatusAsync(string toolId, ToolStatus newStatus, ReasonCode reasonCode)
        {
            var tool = await _toolRepository.GetByIdAsync(toolId);
            if (tool == null) throw new KeyNotFoundException("존재하지 않는 공구입니다.");

            tool.Status = newStatus;

            await _toolHistoryRepository.CreateAsync(new ToolHistory {
                ToolID = toolId,
                ChangeDate = DateTime.Now,
                BeforeCount = tool.CurrentUsageCount,
                Reason = reasonCode,
            });

            await _context.SaveChangesAsync();
        }

        public async Task UseToolAsync(string toolId, int usageAmount, ReasonCode reasonCode)
        {
            var tool = await _toolRepository.GetByIdAsync(toolId);
            if (tool == null) throw new KeyNotFoundException("존재하지 않는 툴입니다.");

            tool.CurrentUsageCount += usageAmount;

            if (tool.CurrentUsageCount >= tool.MaxUsageCount)
            {
                tool.Status = ToolStatus.ReplaceWait; 
            }
            else if (tool.CurrentUsageCount >= (tool.MaxUsageCount * 0.9))
            {
                tool.Status = ToolStatus.Warning;   
            }

            var history = new ToolHistory
            {
                ToolID = toolId,
                ChangeDate = DateTime.Now,
                BeforeCount = tool.CurrentUsageCount - usageAmount,
            };
            await _toolHistoryRepository.CreateAsync(history);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckToolMaintenance(string toolId)
        {
            var tool = await _toolRepository.GetByIdAsync(toolId);
            if (tool == null) throw new KeyNotFoundException("존재하지 않는 툴입니다.");

            return tool.CurrentUsageCount >= (tool.MaxUsageCount * 0.9);
        }

        public async Task<IEnumerable<ToolHistory>> GetToolHistoryAsync(string toolId)
        {
            return await _toolHistoryRepository.GetHistoryByToolIdAsync(toolId);
        }

        public async Task ResetToolCountAsync(string toolId)
        {
            var tool = await _toolRepository.GetByIdAsync(toolId);
            if (tool == null) throw new KeyNotFoundException("존재하지 않는 툴입니다.");

            tool.CurrentUsageCount = 0;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterToolAsync(Tool tool)
        {
            await _toolRepository.CreateAsync(tool);
            await _context.SaveChangesAsync();
        }
    }
}

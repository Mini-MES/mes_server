using mes_server.Models.DTOs.Tool;
using mes_server.Models.Enum;
using mes_server.Models.Production;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolController : ControllerBase
    {
        private readonly IToolService _toolService;
        private readonly IGenericService<Tool> _genericService;
        public ToolController(IToolService toolService, IGenericService<Tool> genericService)
        {
            _toolService = toolService;
            _genericService = genericService;
        }

        // 전체 공구 조회
        [HttpGet]
        public async Task<IActionResult> GetToolStatus()
        {
            return Ok(await _genericService.GetAllAsync());
        }

        // 특정 공구 조회
        [HttpGet("{toolId}")]
        public async Task<IActionResult> GetTool([FromRoute] string toolId)
        {
            var tool = await _genericService.GetByIdAsync(toolId);
            if (tool == null)
            {
                return NotFound(new { Message = "공구를 찾을 수 없습니다." });
            }
            return Ok(tool);
        }

        // 신규 공구 등록
        [HttpPost("register")]
        public async Task<IActionResult> RegisterTool(Tool tool)
        {
            await _toolService.RegisterToolAsync(tool);
            return Ok(new { Message = "공구가 성공적으로 등록되었습니다.", data = tool });
        }

        // 특정 공구의 사용 및 이력 조회
        [HttpGet("{toolId}/history")]
        public async Task<IActionResult> GetToolHistory([FromRoute] string toolId)
        {
            var history = await _toolService.GetToolHistoryAsync(toolId);
            return Ok(history);
        }

        // 공구 변경으로 인한 횟수 초기화
        [HttpPost("reset-count/{toolId}")]
        public async Task<IActionResult> ResetToolCount([FromRoute] string toolId)
        {
            await _toolService.ResetToolCountAsync(toolId);
            return Ok(new { Message = "툴 사용 횟수가 성공적으로 초기화되었습니다." });
        }

        // 특정 공구 상태 변경
        [HttpPut("{toolId}/status")]
        public async Task<IActionResult> UpdateToolStatus([FromRoute] string toolId, [FromBody] ToolStatusUpdateDto toolStatusUpdateDto)
        {
            await _toolService.ChangeToolStatusAsync(toolId, toolStatusUpdateDto.Status, toolStatusUpdateDto.Reason);
            return Ok(new { Message = "공구 상태가 변경되었습니다." });
        }

        // 공구 사용
        [HttpPost("{toolId}/use")]
        public async Task<IActionResult> UseTool([FromRoute] string toolId, [FromBody] ToolUsageDto toolUsageDto)
        {
            await _toolService.UseToolAsync(toolId, toolUsageDto.Amount, toolUsageDto.ReasonCode);
            return Ok(new { Message = "공구 사용량이 기록되었습니다." });
        }
    }
}

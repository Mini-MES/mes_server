using mes_server.Models.DTOs.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEquipments()
        {
            var result = await _equipmentService.GetAllEquipmentAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEquipmentById(string id)
        {
            var result = await _equipmentService.GetEquipmentByIdAsync(id);
            if (result == null) return NotFound("해당 설비를 찾을 수 없습니다.");
            return Ok(result);
        }

        [HttpPost("status")]
        public async Task<IActionResult> ChangeEquipmentStatus([FromBody] ChangeEquipmentStatusRequest request)
        {
            var success = await _equipmentService.ChangeEquipmentStatusAsync(request);
            if (!success) return BadRequest("설비 상태 변경에 실패했습니다.");
            return Ok(new { Message = "설비 상태가 성공적으로 변경되었습니다." });
        }

        [HttpGet("downtime-reasons")]
        public async Task<IActionResult> GetDowntimeReasons()
        {
            var result = await _equipmentService.GetDowntimeReasonsAsync();
            return Ok(result);
        }

        [HttpPost("downtime/reason")]
        public async Task<IActionResult> RegisterDowntimeReason([FromBody] RegisterDowntimeReasonRequest request)
        {
            var success = await _equipmentService.RegisterDowntimeReasonAsync(request);
            if (!success) return BadRequest("비가동 사유 등록에 실패했습니다.");
            return Ok(new { Message = "비가동 사유가 성공적으로 등록되었습니다." });
        }

        [HttpGet("{id}/downtime-history")]
        public async Task<IActionResult> GetDowntimeHistory(string id)
        {
            var result = await _equipmentService.GetDowntimeLogsByEquipmentAsync(id);
            return Ok(result);
        }
    }
}

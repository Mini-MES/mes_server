using mes_server.Models.DTOs.Production;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mes_server.Repositories.Interface.Generic;
using mes_server.Models.Production;

namespace mes_server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionController : ControllerBase
    {
        private readonly IProductionService _productionService;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<Lot> _lotRepository;

        public ProductionController(
            IProductionService productionService,
            IGenericRepository<WorkOrder> workOrderRepository,
            IGenericRepository<Lot> lotRepository)
        {
            _productionService = productionService;
            _workOrderRepository = workOrderRepository;
            _lotRepository = lotRepository;
        }

        // 생산 지시 전체 조회
        [HttpGet("orders")]
        public async Task<IActionResult> GetWorkOrders()
        {
            var result = await _workOrderRepository.GetAllAsync();
            return Ok(result.OrderByDescending(o => o.OrderID));
        }

        // LOT 전체 조회
        [HttpGet("lots")]
        public async Task<IActionResult> GetLots()
        {
            var result = await _lotRepository.GetAllAsync();
            return Ok(result);
        }

        // 생산 지시 생성
        [HttpPost("order")]
        public async Task<IActionResult> CreateWorkOrder([FromBody] WorkOrderCreateDto createDto)
        {
            var result = await _productionService.CreateWorkOrderAsync(createDto);
            return Ok(new { Message = "생산 지시가 성공적으로 생성되었습니다.", data = result });
        }

        // 생산 시작
        [HttpPost("start/{orderId}")]
        public async Task<IActionResult> StartProduction([FromRoute] int orderId)
        {
            var result = await _productionService.StartProductionAsync(orderId);
            return Ok(new { Message = "생산이 성공적으로 시작되었습니다.", data = result });
        }

        // 생산 상태 조회
        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetProductionStatus([FromRoute] int orderId)
        {
            var result = await _productionService.GetProductionStatusAsync(orderId);
            return Ok(new { Message = "생산 상태가 성공적으로 조회되었습니다.", data = result });
        }

        // 생산 완료
        [HttpPost("complete/{orderId}")]
        public async Task<IActionResult> CompleteProduction([FromRoute] int orderId)
        {
            await _productionService.CompleteWorkOrderAsync(orderId);
            return Ok(new { Message = "생산이 성공적으로 완료되었습니다." });
        }

        // 생산 수정
        [HttpPut("order/{orderId}")]
        public async Task<IActionResult> UpdateWorkOrder(int orderId, [FromBody] WorkOrderUpdateDto updateDto)
        {
            await _productionService.UpdateWorkOrderAsync(orderId, updateDto);
            return Ok(new { Message = "생산 지시가 성공적으로 수정되었습니다." });
        }

        // 생산 지시 단일 조회
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetWorkOrder([FromRoute] int orderId)
        {
            var result = await _productionService.GetWorkOrderByIdAsync(orderId);
            return result != null ? Ok(new { Message = "생산 지시가 성공적으로 조회되었습니다.", data = result }) : NotFound("생산 지시를 찾을 수 없습니다.");
        }

        // 생산 삭제
        [HttpDelete("order/{orderId}")]
        public async Task<IActionResult> DeleteWorkOrder(int orderId)
        {
            await _productionService.DeleteWorkOrderAsync(orderId);
            return Ok(new { Message = "생산 지시가 삭제되었습니다." });
        }

        // 공정 이동
        [HttpPost("performance/move")]
        public async Task<IActionResult> MoveProcess([FromBody] PerformanceRegisterDto perfDto, [FromQuery] int nextProcessId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            await _productionService.MoveProcessAsync(perfDto, nextProcessId, userId);
            return Ok(new { Message = "공정 이동이 성공적으로 완료되었습니다." });
        }

        // 단순 실적 등록
        [HttpPost("performance/register")]
        public async Task<IActionResult> RegisterPerformance([FromBody] PerformanceRegisterDto registerDto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            var result = await _productionService.RegisterPerformanceAsync(registerDto, userId);
            return Ok(new { Message = "실적이 성공적으로 등록되었습니다.", data = result });
        }

        // Lot로 현재 상황 조회
        [HttpGet("lot/status/{lotId}")]
        public async Task<IActionResult> GetLotStatus([FromRoute] string lotId)
        {
            var result = await _productionService.GetLotStatusAsync(lotId);
            return Ok(new { Message = "Lot 상태가 성공적으로 조회되었습니다.", data = result });
        }
    }
}

using mes_server.Models.DTOs.Inventory;
using mes_server.Models.History;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;           
        private readonly IGenericService<Shipment> _shipmentService; 
        private readonly IHubContext<MesHub> _hubContext;

        public InventoryController(
            IInventoryService inventoryService,
            IGenericService<Shipment> shipmentService,
            IHubContext<MesHub> hubContext)
        {
            _inventoryService = inventoryService;
            _shipmentService = shipmentService;
            _hubContext = hubContext;
        }

        // --- 1. 비즈니스 로직 (InventoryService 활용) ---

        [HttpPost("update-stock/{materialId}")]
        public async Task<IActionResult> UpdateStock([FromRoute] string materialId, [FromBody] StockUpdateDto dto)
        {
            await _inventoryService.UpdateStockAsync(materialId, dto);

            await _hubContext.Clients.All.SendAsync("StockUpdated", new { MaterialId = materialId, NewStockQty = dto.StockQty });
            return Ok(new { Message = "재고가 업데이트되었습니다." });
        }

        [HttpGet("shipments")]
        public async Task<IActionResult> GetShipmentHistory()
        {
            var result = await _shipmentService.GetAllAsync();
            return Ok(new { Data = result.OrderByDescending(s => s.ShipmentDate) });
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockItems()
        {
            var result = await _inventoryService.GetLowStockMaterialsAsync();
            return Ok(new { Message = "안전 재고 이하 품목 조회 완료", data = result });
        }

        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromBody] InventoryCheckDto checkDto)
        {
            var isAvailable = await _inventoryService.CheckMaterialAvailabilityAsync(checkDto.ProductId, checkDto.TargetQty);
            return Ok(new { Message = "가용 재고 검증 결과", data = isAvailable });
        }

        [HttpPost("ship")]
        public async Task<IActionResult> ShipProduct([FromBody] ShipCreateDto dto)
        {
            await _inventoryService.ShipFinishedProductAsync(
                dto.ProductID, dto.WorkOrderID, dto.Quantity, dto.Destination
            );

            await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new { ProductId = dto.ProductID });
            return Ok(new { Message = "완제품 출하가 완료되었습니다." });
        }
    }
}
using mes_server.Hubs;
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
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IInventoryService inventoryService,
            IGenericService<Shipment> shipmentService,
            IHubContext<MesHub> hubContext,
            ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _shipmentService = shipmentService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // --- 1. 비즈니스 로직 (InventoryService 활용) ---

        [HttpPost("update-stock/{materialId}")]
        public async Task<IActionResult> UpdateStock([FromRoute] string materialId, [FromBody] StockUpdateDto dto)
        {
            await _inventoryService.UpdateStockAsync(materialId, dto);

            try
            {
                await _hubContext.Clients.All.SendAsync("StockUpdated", new { MaterialId = materialId, NewStockQty = dto.StockQty });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR StockUpdated 방송 실패 (DB 처리 정상)");
            }

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

            try
            {
                await _hubContext.Clients.All.SendAsync("WorkOrderUpdated", new { WorkOrderId = dto.WorkOrderID, ProductId = dto.ProductID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR WorkOrderUpdated 방송 실패 (DB 출하 처리 정상)");
            }

            return Ok(new { Message = "완제품 출하가 완료되었습니다." });
        }
    }
}
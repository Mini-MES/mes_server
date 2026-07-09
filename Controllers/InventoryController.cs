using mes_server.Models.DTOs.Inventory;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;           
        private readonly IGenericService<RawMaterial> _materialService; 
        private readonly IGenericService<Shipment> _shipmentService;    

        public InventoryController(
            IInventoryService inventoryService,
            IGenericService<RawMaterial> materialService,
            IGenericService<Shipment> shipmentService)
        {
            _inventoryService = inventoryService;
            _materialService = materialService;
            _shipmentService = shipmentService;
        }

        // --- 1. 단순 CRUD (BaseService 활용) ---

        [HttpGet("materials")]
        public async Task<IActionResult> GetAllMaterials()
        {
            var result = await _materialService.GetAllAsync();
            return Ok(new { Data = result });
        }

        [HttpPost("materials")]
        public async Task<IActionResult> CreateMaterial([FromBody] MaterialCreateDto createDto)
        {
            var result = await _inventoryService.CreateMaterialAsync(createDto);
            return Ok(new { Message = "자재가 등록되었습니다.", data = result });
        }

        [HttpDelete("materials/{materialId}")]
        public async Task<IActionResult> DeleteMaterial([FromRoute] string materialId)
        {
            var material = await _materialService.GetByIdAsync(materialId);
            if (material == null) return NotFound("자재를 찾을 수 없습니다.");

            await _materialService.DeleteAsync(material);
            return Ok(new { Message = "자재가 삭제되었습니다." });
        }

        // --- 2. 비즈니스 로직 (InventoryService 활용) ---

        [HttpPost("update-stock/{materialId}")]
        public async Task<IActionResult> UpdateStock([FromRoute] string materialId, [FromBody] StockUpdateDto dto)
        {
            await _inventoryService.UpdateStockAsync(materialId, dto);
            return Ok(new { Message = "재고가 업데이트되었습니다." });
        }

        [HttpGet("shipments")]
        public async Task<IActionResult> GetShipmentHistory()
        {
            var result = await _shipmentService.GetAllAsync();
            return Ok(new { Data = result.OrderByDescending(s => s.ShipmentDate) });
        }

        [HttpGet("materials/search")]
        public async Task<IActionResult> SearchMaterials([FromQuery] string keyword)
        {
            var result = await _inventoryService.SearchMaterialsAsync(keyword);
            return Ok(new { Data = result });
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
            return Ok(new { Message = "완제품 출하가 완료되었습니다." });
        }
    }
}
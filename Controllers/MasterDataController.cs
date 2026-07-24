using mes_server.Hubs;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataService _masterDataService;
        private readonly IGenericService<BadReasonMaster> _badReasonService;
        private readonly IGenericService<BOM> _bomService;
        private readonly IGenericService<ProductMaster> _productService;
        private readonly IHubContext<MesHub> _hubContext;

        public MasterDataController(
            IMasterDataService masterDataService,
            IGenericService<BadReasonMaster> badReasonService,
            IGenericService<BOM> bomService,
            IGenericService<ProductMaster> productService,
            IHubContext<MesHub> hubContext)
        {
            _masterDataService = masterDataService;
            _badReasonService = badReasonService;
            _bomService = bomService;
            _productService = productService;
            _hubContext = hubContext;
        }

        // 공정 관련 
        [HttpGet("processes")]
        public async Task<IActionResult> GetProcesses() => Ok(await _masterDataService.GetProcessListAsync());

        [HttpPost("processes")]
        public async Task<IActionResult> CreateProcess([FromBody] ProcessCreateDto createDto)
        {
            var process = await _masterDataService.CreateProcessAsync(createDto);
            return Ok(new { Message = "공정이 생성되었습니다.", data = process });
        }

        [HttpPut("processes/{id}")]
        public async Task<IActionResult> UpdateProcess([FromRoute] int id, [FromBody] ProcessUpdateDto updateDto)
        {
            var updatedProcess = await _masterDataService.UpdateProcessAsync(id, updateDto);
            return Ok(new { Message = "공정이 수정되었습니다.", data = updatedProcess });
        }

        // 공정 순서 관련
        [HttpGet("processes/sequence/{sequence}")]
        public async Task<IActionResult> GetProcessBySequence([FromRoute] int sequence)
        {
            var process = await _masterDataService.GetProcessBySequenceAsync(sequence);
            return process != null ? Ok(process) : NotFound();
        }

        // 불량 사유 관련
        [HttpGet("defect-reasons")]
        public async Task<IActionResult> GetBadReasonList()
        {
            var reasons = await _masterDataService.GetBadReasonListAsync();
            return Ok(reasons);
        }

        [HttpGet("defect-reasons/{code}")]
        public async Task<IActionResult> GetBadReasonByCode([FromRoute] ReasonCode code)
        {
            var reasons = await _masterDataService.GetBadReasonByCodeAsync(code);
            return Ok(new { Message = "불량 사유가 성공적으로 조회되었습니다.", data = reasons });
        }

        [HttpPost("defect-reasons")]
        public async Task<IActionResult> CreateDefectReason([FromBody] BadReasonCreateDto createDto)
        {
            var reason = await _masterDataService.CreateBadReasonAsync(createDto);
            return Ok(new { Message = "불량 사유가 생성되었습니다.", data = reason });
        }

        [HttpDelete("defect-reasons/{id}")]
        public async Task<IActionResult> DeleteDefectReason([FromRoute] int id)
        {
            var entity = await _badReasonService.GetByIdAsync(id);
            if (entity == null) return NotFound();
            await _badReasonService.DeleteAsync(entity);
            return NoContent();
        }

        // BOM 관련 
        [HttpGet("bom")]
        public async Task<IActionResult> GetAllBOMs() => Ok(await _bomService.GetAllAsync());

        [HttpGet("bom/{productId}")]
        public async Task<IActionResult> GetBom(string productId) => Ok(await _masterDataService.GetProductBOMAsync(productId));

        [HttpPost("bom")]
        public async Task<IActionResult> AddBom([FromBody] BOMCreateDto createDto)
        {
            var bom = await _masterDataService.AddBomAsync(createDto);
            return Ok(new { Message = "BOM이 성공적으로 추가되었습니다.", data = bom });
        }

        [HttpDelete("bom")]
        public async Task<IActionResult> RemoveBom([FromBody] BOMDeleteDto dto)
        {
            var success = await _masterDataService.DeleteBomAsync(dto.ProductID, dto.ChildProductID, dto.ProcessID);
            if (!success) return NotFound("해당 BOM 데이터를 찾을 수 없습니다.");

            return NoContent();
        }

        // 원자재 및 반제품 전체 조회
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _masterDataService.GetProductsWithBOMAsync();
            return Ok(products);
        }

        // 원자재 및 반제품 특정 조회
        [HttpGet("products/{productId}")]
        public async Task<IActionResult> GetProduct(string productId)
        {
            var product = await _masterDataService.GetProductWithBOMAsync(productId);
            return product != null ? Ok(product) : NotFound();
        }

        // 원자재 및 반제품 생성
        [HttpPost("product")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto createDto)
        {
            var product = await _masterDataService.CreateProductAsync(createDto);

            await _hubContext.Clients.All.SendAsync("ProductCreated", new { Message = "새로운 제품이 생성되었습니다.", data = product });
            return Ok(new { Message = "제품이 성공적으로 생성되었습니다.", data = product });
        }

        // 원자재 및 반제품 삭제
        [HttpDelete("products/{productId}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] string productId)
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product == null) return NotFound();
            await _productService.DeleteAsync(product);
            return NoContent();
        }

        // 원자재 및 반제품 수정
        [HttpPut("products/{productId}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] string productId, [FromBody] ProductUpdateDto updateDto)
        {
            var updatedProduct = await _masterDataService.UpdateProductAsync(productId, updateDto);
            return Ok(new { Message = "제품이 성공적으로 수정되었습니다.", data = updatedProduct });
        }
    }
}
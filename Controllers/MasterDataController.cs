using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataService _masterDataService;
        private readonly IGenericService<ProcessMaster> _processService;
        private readonly IGenericService<BadReasonMaster> _badReasonService;
        private readonly IGenericService<BOM> _bomService;

        public MasterDataController(
            IMasterDataService masterDataService,
            IGenericService<ProcessMaster> processService,
            IGenericService<BadReasonMaster> badReasonService,
            IGenericService<BOM> bomService)
        {
            _masterDataService = masterDataService;
            _processService = processService;
            _badReasonService = badReasonService;
            _bomService = bomService;
        }

        // 공정 관련 
        [HttpGet("processes")]
        public async Task<IActionResult> GetProcesses() => Ok(await _masterDataService.GetProcessListAsync());

        [HttpPost("processes")]
        public async Task<IActionResult> CreateProcess([FromBody] ProcessMaster process)
        {
            await _processService.CreateAsync(process);
            await _processService.SaveChangesAsync();
            return Ok(process);
        }

        [HttpPut("processes/{id}")]
        public async Task<IActionResult> UpdateProcess([FromRoute] int id, [FromBody] ProcessMaster process)
        {
            var processToUpdate = await _processService.GetByIdAsync(id);

            if (processToUpdate == null)
            {
                return NotFound();
            }

            process.ProcessID = id;

            await _processService.UpdateAsync(process);
            await _processService.SaveChangesAsync();
            return Ok(process);
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
            return Ok(new { Message = "불량 사유가 성공적으로 조회되었습니다.", Data = reasons });
        }

        [HttpPost("defect-reasons")]
        public async Task<IActionResult> CreateDefectReason([FromBody] BadReasonMaster reason)
        {
            await _badReasonService.CreateAsync(reason);
            await _badReasonService.SaveChangesAsync();
            return Ok(new { Message = "불량 사유가 성공적으로 생성되었습니다.", Data = reason });
        }

        [HttpDelete("defect-reasons/{id}")]
        public async Task<IActionResult> DeleteDefectReason([FromRoute] int id)
        {
            var entity = await _badReasonService.GetByIdAsync(id);
            if (entity == null) return NotFound();
            await _badReasonService.DeleteAsync(entity);
            await _badReasonService.SaveChangesAsync();
            return NoContent();
        }

        // BOM 관련 
        [HttpGet("bom")]
        public async Task<IActionResult> GetAllBOMs() => Ok(await _bomService.GetAllAsync());

        [HttpGet("bom/{productId}")]
        public async Task<IActionResult> GetBom(string productId) => Ok(await _masterDataService.GetProductBOMAsync(productId));

        [HttpPost("bom")]
        public async Task<IActionResult> AddBom([FromBody] BOM bom)
        {
            await _bomService.CreateAsync(bom);
            await _bomService.SaveChangesAsync();
            return Ok(new { Message = "BOM이 성공적으로 추가되었습니다.", Data = bom });
        }

        [HttpDelete("bom/{bomId}")]
        public async Task<IActionResult> RemoveBom([FromRoute] int bomId)
        {
            var entity = await _bomService.GetByIdAsync(bomId);
            if (entity == null) return NotFound();
            await _bomService.DeleteAsync(entity);
            await _bomService.SaveChangesAsync();
            return NoContent();
        }
    }
}
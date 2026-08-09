using mes_server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiAnalyticsController
    {
        private readonly MESDbContext _context;

        public AiAnalyticsController(MESDbContext context)
        {
            _context = context;
        }

        [HttpGet("ai-analytics")]
        public async Task<IActionResult> GetAiSmartReport()
        {
            var equipments = await _context.Equipments.ToListAsync();
                
            return Ok(equipments);
        }
    }
}

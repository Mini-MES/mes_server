using mes_server.Services;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AiAnalyticsController : ControllerBase
    {
        private readonly GeminiApiService _geminiService;
        private readonly AiPromptBuilder _promptBuilder;

        public AiAnalyticsController(GeminiApiService geminiService, AiPromptBuilder promptBuilder)
        {
            _geminiService = geminiService;
            _promptBuilder = promptBuilder;
        }

        [HttpGet("ai-report")]
        public async Task<IActionResult> GetAiSmartReport()
        {
            string dynamicPrompt = await _promptBuilder.BuildDynamicOeePromptAsync();

            string aiResultMarkdown = await _geminiService.GenerateReportAsync(dynamicPrompt);

            return Ok(new
            {
                Success = true,
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TargetCompany = "(주)태성테크놀로지",
                ReportMarkdown = aiResultMarkdown
            });
        }
    }
}

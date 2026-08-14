using mes_server.Services.AIService;
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

            var result = await _geminiService.GenerateReportAsync(dynamicPrompt);

            if (!result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "AI 스마트 진단 서비스 처리 중 오류가 발생했습니다."
                });
            }

            return Ok(new
            {
                Success = true,
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TargetCompany = "(주)태성테크놀로지",
                ReportMarkdown = result.ReportMarkdown
            });
        }
    }
}

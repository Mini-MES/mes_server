using System.Text;
using System.Text.Json;

namespace mes_server.Services.AIService
{
    public class GeminiReportResult
    {
        public bool IsSuccess { get; set; }
        public bool IsFallback { get; set; }
        public string ReportMarkdown { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public static GeminiReportResult Success(string reportMarkdown, bool isFallback = false)
        {
            return new GeminiReportResult
            {
                IsSuccess = true,
                IsFallback = isFallback,
                ReportMarkdown = reportMarkdown
            };
        }

        public static GeminiReportResult Failure(string errorMessage)
        {
            return new GeminiReportResult
            {
                IsSuccess = false,
                IsFallback = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiApiService> _logger;

        private const string DefaultFallbackReport = @" > ⚠️ **안내**: Gemini API 통신 연결 상태 또는 API 키 설정 상태를 확인해 주세요. 시스템 예시 진단 결과가 표시됩니다.";

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiApiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
            _logger = logger;
        }

        public async Task<GeminiReportResult> GenerateReportAsync(string dynamicPrompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                _logger.LogInformation("Gemini API Key가 유효하지 않거나 미설정되어 Fallback 리포트를 반환합니다.");
                return GeminiReportResult.Success(DefaultFallbackReport, isFallback: true);
            }

            try
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = dynamicPrompt }
                    }
                }
            }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return GeminiReportResult.Success(text ?? DefaultFallbackReport, isFallback: false);
                }

                var errContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API 통신 실패 (Status Code: {StatusCode}, Details: {Details})", response.StatusCode, errContent);

                return GeminiReportResult.Success(DefaultFallbackReport, isFallback: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API 호출 중 예외 발생 - Fallback 리포트로 전환");
                return GeminiReportResult.Success(DefaultFallbackReport, isFallback: true);
            }
        }
    }
}

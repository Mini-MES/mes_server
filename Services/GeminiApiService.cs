using System.Text;
using System.Text.Json;

namespace mes_server.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
        }

        public async Task<string> GenerateReportAsync(string dbDataPrompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "API 키가 설정되지 않아 기본 진단 로직으로 연동되었습니다.";
            }

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] {

                           new
                            {
                                text = dbDataPrompt
                            }}
                    }
                }
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUri, jsonContent);

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
                return text ?? "AI 진단 결과를 가져올 수 없습니다.";
            }
            return "AI 서비스 통신 오류 발생";
        }
    }
}

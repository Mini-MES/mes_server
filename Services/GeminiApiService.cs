using System.Text;
using System.Text.Json;

namespace mes_server.Services
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
                _logger.LogInformation("Gemini API Key가 설정되지 않아 Fallback 리포트를 반환합니다.");
                string fallbackText = @"# 🤖 (주)태성테크놀로지 AI 스마트 생산 진단 리포트

> ⚠️ **안내**: Gemini API 키가 설정되지 않아 시스템 예시 진단 결과가 표시됩니다. `appsettings.json`에 `Gemini:ApiKey`를 설정하시면 구글 Gemini AI가 실시간으로 분석한 결과를 받아보실 수 있습니다.

---

### 📊 1. 전사 설비 종합 평가
- **전사 평균 OEE**: **79.0%** (업계 평균 60~70% 상회)
- **목표 대비 현황**: World Class 기준(85.0%) 대비 **-6.0%p 미달**
- **라인 현황**: CNC01, CNC02, CNC04는 가동률 93% 이상으로 양호하나, **CNC03(밀링 #1)**의 극심한 저조로 전사 생산성에 병목 발생.

---

### 🚨 2. 최저 OEE 병목 설비 진단: CNC03 (CNC 밀링 #1)
- **OEE 수치**: **59.2%** (전사 최저)
- **총 비가동시간**: **3,388분** (전사 총 비가동시간의 **48.5%** 차지)
- **핵심 원인 분석**:
  1. **반복 셋업 손실 (1,199분, 35.4%)**: 지그교체, 공구세팅, 프로그램 변경 반복 발생 (SMED 셋업 표준화 부재)
  2. **반복 설비고장 (1,104분, 32.6%)**: 10/7(372분), 10/8(439분) 스핀들 이상으로 2회 대형 셧다운 발생
  3. **자재 대기 손실 (625분, 18.4%)**: 자재 불출 지연 및 전공정(선삭) 공급 대기

---

### 💡 3. 우선순위별 엔지니어링 개선 대책

#### 🥇 1순위 (즉시 적용 / 저비용)
* **SOP 셋업 표준절차서 수립 & 외부 셋업(SMED) 분리**: 지그/공구 사전준비 체크리스트 작성으로 셋업시간 40% 단축
* **자재 사전 키팅(Kitting) 체계 구축**: 전날 자재 사전 키팅 배치로 자재대기 625분 즉시 절감

#### 🥈 2순위 (중기 프로세스 구축)
* **스핀들 예방보전(PM) 체계 구축**: 정기 점검주기 수립 및 고장이력 데이터화로 돌발 셧다운 70% 예방
* **동일 품번 묶음 생산(Batching)**: 불필요한 셋업 교체 횟수 최소화

#### 🥉 3순위 (중장기 / 설비 투자)
* **CBM 상태기반 예지보전 센서 도입**: 스핀들 진동/온도 센서 부착으로 돌발 정지 사전 알람 연동
* **공정 부하 재분배**: CNC03의 밀링 부하 일부를 가동률 여유가 있는 CNC04로 이관";

                return GeminiReportResult.Success(fallbackText, isFallback: true);
            }

            try
            {
                var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

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

                    return GeminiReportResult.Success(text ?? "AI 진단 결과를 반환받지 못했습니다.", isFallback: false);
                }

                _logger.LogError("Gemini API 통신 실패 (Status Code: {StatusCode})", response.StatusCode);
                return GeminiReportResult.Failure("AI API 서비스 응답 오류가 발생했습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API 호출 중 예외 발생");
                return GeminiReportResult.Failure("AI API 호출 처리 중 예외가 발생했습니다.");
            }
        }
    }
}

using mes_server.Data;
using mes_server.Services.EquipmentService;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace mes_server.Services.AIService
{
    public class AiPromptBuilder
    {
        private readonly IEquipmentService _equipmentService;
        private readonly MESDbContext _context;

        public AiPromptBuilder(IEquipmentService equipmentService, MESDbContext context)
        {
            _equipmentService = equipmentService;
            _context = context;
        }
        public async Task<string> BuildDynamicOeePromptAsync()
        {
            var oeeSummary = await _equipmentService.GetOEESummaryAsync();

            var sb = new StringBuilder();
            sb.AppendLine("너는 자동차 정밀가공 제조기업 (주)태성테크놀로지의 전문 MES AI 컨설턴트야.");
            sb.AppendLine("아래는 시스템의 EquipmentService에서 방금 실시간으로 계산된 전사 OEE 수치 및 비가동 데이터야. 이를 바탕으로 [1. 종합 평가, 2. 최저 OEE 병목 설비 원인 진단, 3. 엔지니어링 개선 대책]을 전문적인 한국어 마크다운 리포트로 작성해줘.");
            sb.AppendLine();
            sb.AppendLine($"[1. 전사 OEE 요약 현황 (전체 OEE: {oeeSummary.OverallOee}%, 가동률: {oeeSummary.AverageAvailability}%, 성능: {oeeSummary.AveragePerformance}%, 양품률: {oeeSummary.AverageQuality}%)]");

            foreach (var eq in oeeSummary.Equipments)
            {
                sb.AppendLine($"- {eq.EquipmentID} ({eq.EquipmentName}): 가동률 {eq.AvailabilityRate}%, 성능효율 {eq.PerformanceRate}%, 양품률 {eq.QualityRate}%, OEE {eq.OeePercentage}% (비가동: {eq.TotalDowntimeMinutes}분)");
            }

            var bottleneck = oeeSummary.Equipments.OrderBy(e => e.OeePercentage).FirstOrDefault();

            if (bottleneck != null)
            {
                sb.AppendLine();
                sb.AppendLine($"[2. 실시간 최저 OEE 병목 설비: {bottleneck.EquipmentID} ({bottleneck.EquipmentName})]");
                sb.AppendLine($"- 현재 OEE: {bottleneck.OeePercentage}% (가동률: {bottleneck.AvailabilityRate}%, 성능: {bottleneck.PerformanceRate}%, 양품률: {bottleneck.QualityRate}%)");
                sb.AppendLine($"- 총 비가동시간: {bottleneck.TotalDowntimeMinutes}분");

                var topReasons = await _context.DowntimeLogs
                    .AsNoTracking()
                    .Where(d => d.EquipmentID == bottleneck.EquipmentID && d.ReasonCode != null)
                    .GroupBy(d => d.DowntimeReason != null ? d.DowntimeReason.ReasonName : d.ReasonCode)
                    .Select(g => new
                    {
                        Reason = g.Key ?? "기타 사유",
                        TotalMinutes = (int)(g.Sum(x => x.DurationSeconds ?? 0) / 60),
                        Count = g.Count()
                    })
                    .OrderByDescending(r => r.TotalMinutes)
                    .Take(3)
                    .ToListAsync();

                sb.AppendLine("- 주요 비가동 원인 Top 3 (DB 실시간 GroupBy 집계):");
                int rank = 1;
                foreach (var r in topReasons)
                {
                    sb.AppendLine($"  {rank++}위: {r.Reason} -> 총 {r.TotalMinutes}분 정지 ({r.Count}회 발생)");
                }
            }

            sb.AppendLine();
            sb.AppendLine("[3. 리포트 작성 지침]");
            sb.AppendLine("- Markdown(마크다운) 형식을 사용하여 모달 UI에서 시각적으로 직관적이게 구성해줘.");
            sb.AppendLine("- 단기 즉시 조치(SMED 셋업 표준화, 자재 사전 키팅), 중기 체계 구축(스핀들 예방보전 PM), 장기 상태점검(CBM 예지보전 센서 및 부하 재분배) 개선안을 구분하여 제시해줘.");

            return sb.ToString();
        }
    }
}

using mes_server.Models.MasterData;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.Analytics
{
    [Index(nameof(EquipmentID), nameof(WorkDate), IsUnique = true)]
    public class DailyEquipmentProduction
    {
        [Key]
        public int DailyEquipmentOeeID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EquipmentID { get; set; } = null!;

        [ForeignKey("EquipmentID")]
        public Equipment? Equipment { get; set; }

        [Required]
        public DateOnly WorkDate { get; set; }

        public int PlannedProductionMinutes { get; set; }

        public int OperatingMinutes { get; set; }

        public int DowntimeMinutes { get; set; }

        public int TotalProducedQty { get; set; }

        public int GoodQty { get; set; }

        public int DefectQty { get; set; }

        [Column(TypeName = "decimal(10, 4)")]
        public decimal IdealCycleTimeMinutes { get; set; }

        // ====================================================
        // 💡 OEE 3대 지표 자동 계산용 Domain NotMapped 프로퍼티
        // ====================================================

        /// <summary> 시간 가동률 (%) = 실가동시간 / 계획가동시간 * 100 </summary>
        [NotMapped]
        public double AvailabilityRate => PlannedProductionMinutes > 0
            ? Math.Min(100.0, Math.Round((double)OperatingMinutes / PlannedProductionMinutes * 100.0, 1))
            : 0.0;

        /// <summary> 성능 효율 (%) = (생산수량 * 이론 사이클타임) / 실가동시간 * 100 </summary>
        [NotMapped]
        public double PerformanceRate => OperatingMinutes > 0
            ? Math.Min(100.0, Math.Round(((double)IdealCycleTimeMinutes * TotalProducedQty) / OperatingMinutes * 100.0, 1))
            : 0.0;

        /// <summary> 양품률 (%) = 양품수량 / 생산수량 * 100 </summary>
        [NotMapped]
        public double QualityRate => TotalProducedQty > 0
            ? Math.Round((double)GoodQty / TotalProducedQty * 100.0, 1)
            : 100.0;

        /// <summary> 설비 종합 효율 OEE (%) = 가동률 * 성능효율 * 양품률 / 10,000 </summary>
        [NotMapped]
        public double OeePercentage => Math.Round((AvailabilityRate * PerformanceRate * QualityRate) / 10000.0, 1);
    }
}

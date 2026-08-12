using mes_server.Models.MasterData;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.Analytics
{
    public class DailyEquipmentOee
    {
        [Key]
        public int DailyEquipmentOeeID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EquipmentID { get; set; } = null!;

        [ForeignKey(nameof(EquipmentID))]
        public Equipment? Equipment { get; set; }

        [Required]
        public DateOnly WorkDate { get; set; }

        [Range(0, int.MaxValue)]
        public int PlannedProductionMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int OperatingMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int DowntimeMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int TargetQty { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalProducedQty { get; set; }

        [Range(0, int.MaxValue)]
        public int GoodQty { get; set; }

        [Range(0, int.MaxValue)]
        public int DefectQty { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AvailabilityRate { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PerformanceRate { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal QualityRate { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal OeePercentage { get; set; }

        [Required]
        [MaxLength(260)]
        public string SourceFileName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ImportBatchID { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int SourceRowNumber { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}

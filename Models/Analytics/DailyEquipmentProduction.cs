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

        public decimal IdealCycleTimeMinutes { get; set; }
    }
}

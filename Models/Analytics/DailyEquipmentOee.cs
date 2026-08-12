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

        public Equipment? Equipment { get; set; }

        [Required]
        [StringLength(50)]
        public string EquipmentName { get; set; } = null!;

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

using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class Equipment
    {
        [Key]
        [MaxLength(50)]
        public string EquipmentID { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = EquipmentStatus.Stopped;

        [MaxLength(50)]
        public string? CurrentLotId { get; set; }

        public string? CurrentOperatorId { get; set; }

        public User? CurrentOperator { get; set; }

        public long TotalRunningSeconds { get; set; } = 0;

        public long TotalDowntimeSeconds { get; set; } = 0;

        public DateTime LastStatusChangedAt { get; set; } = DateTime.UtcNow;

    }
}

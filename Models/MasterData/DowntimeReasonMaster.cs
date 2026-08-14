using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class DowntimeReasonMaster
    {
        [Key]
        [MaxLength(50)]
        public string ReasonCode { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ReasonName { get; set; } = null!;

        [MaxLength(50)]
        public string Category { get; set; } = "GENERAL";
    }
}

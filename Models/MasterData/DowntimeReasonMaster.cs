using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class DowntimeReasonMaster
    {
        [Key]
        [MaxLength(50)]
        public string ReasonCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReasonName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "GENERAL";

        public bool IsActive { get; set; } = true; // 비가동 사유 사용 여부
    }
}

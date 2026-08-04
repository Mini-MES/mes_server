using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.MasterData
{
    public class DowntimeLog
    {
        [Key]
        public int DowntimeLogID { get; set; }

        [Required]
        [MaxLength(20)]
        public string EquipmentID { get; set; } = null!;

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow; // 비가동 시작 일시                                                                                                             

        public DateTime? EndedAt { get; set; } // 비가동 종료 일시                                                                                                                                 

        public int? DurationSeconds { get; set; }                                                                                                           

        [MaxLength(50)]
        public string? ReasonCode { get; set; } // 선택한 비가동 사유 코드                                                                                                                         

        public string? OperatorMemo { get; set; } // 작업자 메모                                                                                                                                   

        [MaxLength(50)]
        public string? UserID { get; set; } // 등록한 작업자 ID                                                                                                                                    
                                                                                                                                
        [ForeignKey("EquipmentID")]
        public Equipment? Equipment { get; set; }

        [ForeignKey("ReasonCode")]
        public DowntimeReasonMaster? DowntimeReason { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}

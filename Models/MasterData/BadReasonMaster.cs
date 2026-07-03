using mes_server.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class BadReasonMaster
    {
        [Key]
        [MaxLength(20)]
        public ReasonCode ReasonCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReasonDescription { get; set; } = null!;
    }
}

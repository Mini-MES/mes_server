using mes_server.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class BadReasonMaster
    {
        [Key]
        [Required]
        public ReasonCode ReasonCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReasonDescription { get; set; } = null!;
    }
}

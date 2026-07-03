using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class BadReasonMaster
    {
        [Key]
        [MaxLength(20)]
        public string ReasonCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReasonDescription { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class ProcessMaster
    {
        [Key]
        public int ProcessID { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProcessName { get; set; } = null!;

        [Required]
        public int SequenceOrder { get; set; }
    }
}

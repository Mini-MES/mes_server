using mes_server.Models.Production;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.History
{
    public class ToolHistory
    {
        [Key]
        [Required]
        public int HistoryID { get; set; }

        [Required]
        [MaxLength(20)]
        public string ToolID { get; set; } = null!;

        [ForeignKey("ToolID")]
        public Tool? Tool { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public int BeforeCount { get; set; }

        [MaxLength(20)]
        public string? Reson { get; set; }
    }
}

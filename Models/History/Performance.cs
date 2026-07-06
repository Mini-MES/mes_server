using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.History
{
    public class Performance
    {
        [Key]
        [Required]
        public int PerfID { get; set; }

        [Required]
        public int WorkOrderID { get; set; }
        [ForeignKey("WorkOrderID")]
        public WorkOrder? WorkOrder { get; set; }

        [Required]
        [MaxLength(20)]
        public string LotID { get; set; } = null!;
        [ForeignKey("LotID")]
        public Lot? Lot { get; set; }

        [Required]
        public int ProcessID { get; set; }
        [ForeignKey("ProcessID")]
        public ProcessMaster? Process { get; set; }

        [Required]
        [MaxLength(20)]
        public string? ToolID { get; set; }
        [ForeignKey("ToolID")]
        public Tool? Tool { get; set; }

        [Required]
        public ReasonCode ReasonCode { get; set; }
        [ForeignKey("ReasonCode")]
        public BadReasonMaster? BadReason { get; set; }

        [Required]
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User? User { get; set; }

        [Required]
        public int InputQty { get; set; }
        [Required]
        public int GoodQty { get; set; }
        [Required]
        public int BadQty { get; set; }
        [Required]
        public DateTime WorkDate { get; set; }
    }
}
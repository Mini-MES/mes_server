using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public WorkOrder? WorkOrder { get; set; }

        [Required]
        [MaxLength(20)]
        public string LotID { get; set; } = null!;
        [ForeignKey("LotID")]
        [JsonIgnore]
        public Lot? Lot { get; set; }

        [Required]
        public int ProcessID { get; set; }
        [ForeignKey("ProcessID")]
        [JsonIgnore]
        public ProcessMaster? Process { get; set; }

        [MaxLength(20)]
        public string? ToolID { get; set; }
        [ForeignKey("ToolID")]
        [JsonIgnore]
        public Tool? Tool { get; set; }

        public ReasonCode? ReasonCode { get; set; }
        [ForeignKey("ReasonCode")]
        [JsonIgnore]
        public BadReasonMaster? BadReason { get; set; }

        [Required]
        public string UserID { get; set; } = null!;
        [ForeignKey("UserID")]
        [JsonIgnore]
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
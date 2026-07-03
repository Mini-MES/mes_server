using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.Production
{
    public class Lot
    {
        [Key]
        public string LotID { get; set; } = string.Empty;

        [Required]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public WorkOrder? WorkOrder { get; set; }

        [Required]
        public int CurrentProcessID { get; set; }

        [ForeignKey("CurrentProcessID")]
        public ProcessMaster? CurrentProcess { get; set; }

        [Required]
        [MaxLength(20)]
        public LotStatus Status { get; set; }
    }
}

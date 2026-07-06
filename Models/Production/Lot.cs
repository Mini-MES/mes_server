using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.Production
{
    public class Lot
    {
        [Key]
        [MaxLength(20)]
        public string LotID { get; set; } = null!;

        [Required]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public WorkOrder? WorkOrder { get; set; }

        [Required]
        public int CurrentProcessID { get; set; }

        [ForeignKey("CurrentProcessID")]
        public ProcessMaster? CurrentProcess { get; set; }

        public int TotalBadQty { get; set; }

        [Required]
        public LotStatus Status { get; set; }
    }
}

using mes_server.Models.Production;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.History
{
    public class Shipment
    {
        [Key]
        public int ShipmentID { get; set; }

        [Required]
        public string ProductID { get; set; } = null!;

        [ForeignKey("ProductID")]
        public string Product { get; set; } = null!;
        [Required]
        public int WorkOrderID { get; set; }

        [ForeignKey("WorkOrderID")]
        public virtual WorkOrder WorkOrder { get; set; } = null!;

        [Required] public int Quantity { get; set; }
        [Required] public string Destination { get; set; } = null!;
        public DateTime ShipmentDate { get; set; } = DateTime.Now;
    }
}
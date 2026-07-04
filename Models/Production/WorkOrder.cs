using mes_server.Models.MasterData;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.Production
{
    public class WorkOrder
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProductID { get; set; } = null!;

        [ForeignKey("ProductID")]
        public ProductMaster? Product { get; set; }

        public ICollection<Lot> Lots { get; set; } = new List<Lot>();

        [Required]
        public int TargetQty { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }
    }
}

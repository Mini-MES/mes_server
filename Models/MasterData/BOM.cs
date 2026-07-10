using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.MasterData
{
    public class BOM
    {
        [Required]
        [MaxLength(50)]
        public string ProductID { get; set; } = null!;

        [ForeignKey("ProductID")]
        public ProductMaster? Product { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaterialID { get; set; } = null!;

        [Required]
        public int RequiredQty { get; set; }

        [Required]
        public int ProcessID { get; set; }

        [ForeignKey("ProcessID")]
        public ProcessMaster? Process { get; set; }
    }
}

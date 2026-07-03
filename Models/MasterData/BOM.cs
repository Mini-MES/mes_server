using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mes_server.Models.MasterData
{
    public class BOM
    {
        [Required]
        [MaxLength(50)]
        public string ProductID { get; set; } = string.Empty;

        [ForeignKey("ProductID")]
        public ProductMaster? Product { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaterialID { get; set; } = string.Empty;

        [ForeignKey("MaterialID")]

        public RawMaterial? Material { get; set; }
        [Required]
        public int RequiredQty { get; set; }

    }
}

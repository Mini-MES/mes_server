using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace mes_server.Models.MasterData
{
    public class BOM
    {
        [Required]
        [MaxLength(50)]
        public string ProductID { get; set; } = null!;

        [ForeignKey("ProductID")]
        [JsonIgnore]
        public ProductMaster? Product { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChildProductID { get; set; } = null!;

        [ForeignKey("ChildProductID")]
        public ProductMaster? ChildProduct { get; set; }

        [Required]
        public int RequiredQty { get; set; }

        [Required]
        public int ProcessID { get; set; }

        [ForeignKey("ProcessID")]
        public ProcessMaster? Process { get; set; }
    }
}

using mes_server.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class ProductMaster
    {
        [Key]
        [MaxLength(50)]
        public string ProductID { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = null!;

        public ICollection<BOM> BOMs { get; set; } = new List<BOM>();

        [Required]
        public ItemType ItemType { get; set; } // 완제품/반제품을 나누는 역할

        [Required]
        public int StockQty { get; set; }

        [Required]
        public int SafetyStock { get; set; }
    }
}

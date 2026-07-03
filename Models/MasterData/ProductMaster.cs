using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class ProductMaster
    {
        [Key]
        [MaxLength(50)]
        public string ProductID { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ItemType { get; set; } = string.Empty; // 완제품/반제품을 나누는 역할

        [Required]
        public int StockQty { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class RawMaterial
    {
        [Key]
        [MaxLength(20)]
        public string MaterialID { get; set; } = null!;

        [Required]
        [MaxLength(50)] 
        public string MaterialName { get; set; } = null!;

        [Required]
        public int StockQty { get; set; }
    }
}

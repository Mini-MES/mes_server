using mes_server.Models.Enum;
using System.Collections.Generic;

namespace mes_server.Models.DTOs.MasterData
{
    public class ProductResponseDto
    {
        public string ProductID { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int StockQty { get; set; }
        public int SafetyStock { get; set; }
        public ItemType ItemType { get; set; }
        public ICollection<BOMResponseDto> BOMs { get; set; } = new List<BOMResponseDto>();
    }
}

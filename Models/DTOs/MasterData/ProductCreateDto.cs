using mes_server.Models.Enum;

namespace mes_server.Models.DTOs.MasterData
{
    public class ProductCreateDto
    {
        public string ProductID { get; set; } = null!;
        public string ProductName { get; set; } = null!;

        public ItemType ItemType { get; set; }
        public int StockQty { get; set; }
        public int SafetyStock { get; set; }
    }
}

namespace mes_server.Models.DTOs.Inventory
{
    public class StockUpdateDto
    {
        public int StockQty { get; set; }
        public string MaterialName { get; set; } = null!;
        public int SafetyStock { get; set; }
    }
}

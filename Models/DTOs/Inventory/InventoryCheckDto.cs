namespace mes_server.Models.DTOs.Inventory
{
    public class InventoryCheckDto
    {
        public string ProductId { get; set; } = null!;
        public int TargetQty { get; set; }
    }
}

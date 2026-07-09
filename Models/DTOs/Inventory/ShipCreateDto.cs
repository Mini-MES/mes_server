namespace mes_server.Models.DTOs.Inventory
{
    public class ShipCreateDto
    {
        public string ProductID { get; set; } = null!;
        public int WorkOrderID { get; set; }
        public int Quantity { get; set; }
        public string Destination { get; set; } = null!;
    }
}

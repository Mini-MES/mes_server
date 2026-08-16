namespace mes_server.Models.DTOs.Production
{
    public class StartProductionResponseDto
    {
        public int WorkOrderID { get; set; }
        public string LotID { get; set; } = null!;
        public string EquipmentID { get; set; } = null!;
    }
}

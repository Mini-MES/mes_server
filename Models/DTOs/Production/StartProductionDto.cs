namespace mes_server.Models.DTOs.Production
{
    public class StartProductionDto
    {
        public string LotId { get; set; } = null!;
        public string EquipmentID { get; set; } = null!;
    }
}

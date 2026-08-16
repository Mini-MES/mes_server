namespace mes_server.Models.DTOs.Production
{
    public class StartProductionDto
    {
        public string lotId { get; set; } = null!;
        public string EquipmentID { get; set; } = null!;
    }
}

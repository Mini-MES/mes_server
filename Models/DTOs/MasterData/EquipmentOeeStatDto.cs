namespace mes_server.Models.DTOs.MasterData
{
    public class EquipmentOeeStatDto
    {
        public string EquipmentID { get; set; } = null!;
        public string Name { get; set; } = null!;
        public double CycleTimeMinutes { get; set; }
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double Oee { get; set; }
        public double TotalDowntimeMinutes { get; set; }
    }
}

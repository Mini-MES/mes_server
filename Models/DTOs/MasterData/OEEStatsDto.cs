namespace mes_server.Models.DTOs.MasterData
{
    public class EquipmentOeeDto
    {
        public string EquipmentID { get; set; } = null!;
        public string EquipmentName { get; set; } = null!;
        public string Status { get; set; } = null!;
        
        public double TotalRunningMinutes { get; set; }
        public double TotalDowntimeMinutes { get; set; }

        public int TargetQty { get; set; }
        public int TotalProducedQty { get; set; }
        public int GoodQty { get; set; }
        public int DefectQty { get; set; }

        public double AvailabilityRate { get; set; }
        public double PerformanceRate { get; set; }
        public double QualityRate { get; set; }
        public double OeePercentage { get; set; }
    }

    public class OEESummaryDto
    {
        public double OverallOee { get; set; }
        public double AverageAvailability { get; set; }
        public double AveragePerformance { get; set; }
        public double AverageQuality { get; set; }

        public int TotalEquipments { get; set; }
        public int RunningEquipments { get; set; }
        public int StoppedEquipments { get; set; }

        public List<EquipmentOeeDto> Equipments { get; set; } = new();
    }
}
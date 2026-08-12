namespace mes_server.Models.DTOs.Analytics
{
    public class DailyEquipmentProductionDto
    {
        public int DailyEquipmentOeeID { get; set; }
        public DateOnly WorkDate { get; set; }
        public string EquipmentID { get; set; } = null!;
        public string? EquipmentName { get; set; }
        public int PlannedProductionMinutes { get; set; }
        public int OperatingMinutes { get; set; }
        public int DowntimeMinutes { get; set; }
        public int TotalProducedQty { get; set; }
        public int GoodQty { get; set; }
        public int DefectQty { get; set; }
        public decimal IdealCycleTimeMinutes { get; set; }

        // OEE 3대 지표 산출 결과
        public double AvailabilityRate { get; set; }
        public double PerformanceRate { get; set; }
        public double QualityRate { get; set; }
        public double OeePercentage { get; set; }
    }
}

namespace mes_server.Models.DTOs.MasterData
{
    public class EquipmentOeeDto
    {
        public string EquipmentId { get; set; } = null!;
        public string EquipmentName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public double TotalRunningMinutes { get; set; }
        public double TotalDowntimeMinutes { get; set; }
        public double AvailabilityRate { get; set; } // 가동률
        public double PerformanceRate { get; set; } // 성능률
        public double QualityRate { get; set; } // 품질률
        public double OEE { get; set; } // OEE
    }

    public class OEESummaryDto
    {
        public double AverageAvailabilityRate { get; set; } 
        public double AveragePerformanceRate { get; set; } // 평균 성능률
        public double AverageQualityRate { get; set; } // 평균 품질률
        public double AverageOEE { get; set; } // 평균 OEE

        public int TotalEquipmentCount { get; set; } // 총 장비 수
        public int RunningEquipmentCount { get; set; }
        public int StoppedEquipmentCount { get; set; }
    }
}

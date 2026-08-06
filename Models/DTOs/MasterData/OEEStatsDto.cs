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

        public double AvailabilityRate { get; set; } // 가동률 (%)                                                                                                
        public double PerformanceRate { get; set; }  // 성능률 (%)                                                                                                
        public double QualityRate { get; set; }      // 품질률 (%)                                                                                                
        public double OeePercentage { get; set; }   // 종합 OEE (%)                                                                                               
    }

    public class OEESummaryDto
    {
        public double OverallOee { get; set; }              // 공장 전체 평균 OEE (%)                                                                             
        public double AverageAvailability { get; set; }     // 평균 가동률 (%)                                                                                    
        public double AveragePerformance { get; set; }      // 평균 성능률 (%)                                                                                    
        public double AverageQuality { get; set; }          // 평균 품질률 (%)                                                                                    

        public int TotalEquipments { get; set; }           // 총 설비 수                                                                                          
        public int RunningEquipments { get; set; }         // 가동 중 설비 수                                                                                     
        public int StoppedEquipments { get; set; }         // 비가동 설비 수                                                                                      

        // CodeRabbit 리뷰 반영 설비별 상세 목록                                                                                                                  
        public List<EquipmentOeeDto> Equipments { get; set; } = new();
    }
}
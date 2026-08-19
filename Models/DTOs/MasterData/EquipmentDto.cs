namespace mes_server.Models.DTOs.MasterData
{
    // 설비 상태 조회 응답 DTO                                                                                                                                                                     
    public class EquipmentDto
    {
        public string EquipmentID { get; set; } = null!;
        public string EquipmentName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? CurrentLotID { get; set; }
        public string? CurrentOperatorID { get; set; }
        public long TotalRunningSeconds { get; set; }
        public long TotalDowntimeSeconds { get; set; }
        public DateTime LastStatusChangedAt { get; set; }
    }

    // 설비 상태 변경 요청 DTO                                                                                                                                                                     
    public class ChangeEquipmentStatusRequest
    {
        public string EquipmentID { get; set; } = null!;
        public string NewStatus { get; set; } = null!;                                                                                              
        public string? CurrentLotID { get; set; }
    }

    // 작업자 비가동 사유 등록 요청 DTO                                                                                                                                                            
    public class RegisterDowntimeReasonRequest
    {
        public int DowntimeLogID { get; set; }
        public string ReasonCode { get; set; } = null!;
        public string? OperatorMemo { get; set; }
        public string? UserID { get; set; }
    }

    // 비가동 사유 마스터 조회 응답 DTO                                                                                                                                                            
    public class DowntimeReasonDto
    {
        public string ReasonCode { get; set; } = null!;
        public string ReasonName { get; set; } = null!;
        public string Category { get; set; } = null!;
    }
}

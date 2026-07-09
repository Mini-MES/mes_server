namespace mes_server.Models.Enum
{
    public enum ToolStatus
    {
        Ready,       // [사용 가능] 공구함에 보관 중, 장착 대기 상태
        InUse,       // [사용 중] 설비에 장착되어 작업 중
        Warning,     // [교체 임박] 수명의 90% 이상 도달, 곧 교체 필요
        ReplaceWait, // [교체 대기] 수명 다함, 즉시 교체 필요
        Repair,      // [정비 중] 연마(Sharpening) 또는 수리 중
        Disposed     // [폐기] 수명 종료 또는 파손으로 사용 불가
    }
}

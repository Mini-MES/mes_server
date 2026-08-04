namespace mes_server.Models.MasterData
{
    public static class EquipmentStatus
    {
        public const string Running = "RUNNING"; // 가동 중
        public const string Stopped = "STOPPED"; // 정지
        public const string Maintenance = "MAINTENANCE"; // 유지보수
        public const string Idle = "IDLE"; // 유휴
        public const string Error = "ERROR"; // 오류
        public const string Setup = "SETUP"; // 셋업/교체 중

        // 추후, 상태가 늘어나면 이곳에 추가
    }
}

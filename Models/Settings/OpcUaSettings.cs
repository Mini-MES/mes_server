namespace mes_server.Models.Settings
{
    public class OpcUaSettings
    {
        public string ServerURL { get; set; } = null!;
        public string ApplicationName { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool AutoAcceptCertificates { get; set; }
        public int SessionTimeout { get; set; }
        public int KeepAliveInterval { get; set; }

        public List<OpcUaEquipmentSettings> Equipments { get; set; } = [];
    }

    public class OpcUaEquipmentSettings
    {
        public string EquipmentId { get; set; } = null!;
        public string CounterNodeId { get; set; } = null!;
        public string RunningNodeId { get; set; } = null!;
        public string TemperatureNodeId { get; set; } = null!;
    }
}

namespace mes_server.Models.Settings
{
    public class OpcUaSettings
    {
        public string ServerURL { get; set; } = null!;
        public string ApplicationName { get; set; } = null!;
        public bool AutoAcceptCertificates { get; set; }
        public int SessionTimeout { get; set; }
        public int KeepAliveInterval { get; set; }
    }
}

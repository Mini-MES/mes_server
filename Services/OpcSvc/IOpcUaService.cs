namespace mes_server.Services.OpcSvc
{
    public interface IOpcUaService
    {
        event Action<string, object, DateTime>? OnDataReceived;
        Task ConnectAndSubscribeAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
    }
}


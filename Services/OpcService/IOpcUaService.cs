namespace mes_server.Services.OpcService
{
    public interface IOpcUaService
    {
        event Action<string, object, DateTime>? OnDataReceived;
        Task ConnectAndSubscribeAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
    }
}


namespace mes_server.Services.Interface
{
    public interface IOpcUaService
    {
        event Action<string, object, DateTime> DataChanged;
        Task ConnectAndSubscribeAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
    }
}

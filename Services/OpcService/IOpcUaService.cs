using mes_server.Models.Enum;

namespace mes_server.Services.OpcService
{
    public interface IOpcUaService
    {
        event Action<OpcUaTagEvent>? OnDataReceived;
        Task ConnectAndSubscribeAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
    }
}


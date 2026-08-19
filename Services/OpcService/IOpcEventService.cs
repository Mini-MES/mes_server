namespace mes_server.Services.OpcService
{
    public interface IOpcEventService
    {
        Task HandleTagChangedAsync(OpcUaTagEvent tagEvent, long counterDelta = 0);
    }
}

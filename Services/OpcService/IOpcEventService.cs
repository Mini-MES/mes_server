namespace mes_server.Services.OpcService
{
    public interface IOpcEventService
    {
        Task HandleTagChangedAsync(OpcUaTagType tagEvent, long counterDelta = 0);
    }
}

namespace mes_server.Services.OpcService
{
    public interface IOpcEventService
    {
        Task HandleTagChangedAsync(string tagName, object value, DateTime timestamp);
    }
}

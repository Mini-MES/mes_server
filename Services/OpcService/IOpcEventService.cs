using mes_server.Models.Enum;

namespace mes_server.Services.OpcService
{
    public interface IOpcEventService
    {
        Task HandleTagChangedAsync(string EquipmentId, OpcUaTagType TagType, string NodeId, object value, DateTime timestamp);
    }
}

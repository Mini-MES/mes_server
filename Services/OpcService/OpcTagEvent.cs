namespace mes_server.Services.OpcService
{
    public enum OpcUaTagType
    {
        Counter,
        Running,
        Temperature
    }

    public record OpcUaTagEvent(
        string EquipmentId,
        OpcUaTagType TagType,
        string NodeId,
        object Value,
        DateTime Timestamp);

    public record OpcUaMonitoredItemContext(
        string EquipmentId,
        OpcUaTagType TagType,
        string NodeId);
}
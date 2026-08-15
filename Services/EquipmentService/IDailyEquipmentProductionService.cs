namespace mes_server.Services.EquipmentService
{
    public interface IDailyEquipmentProductionService
    {
        Task CreateDailyEquipmentProductionAsync(string targetEquipmentId, DateOnly today, int goodQty, int badQty, bool autoSave = true);
    }
}

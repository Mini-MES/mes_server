namespace mes_server.Services.EquipmentService
{
    public interface IDailyEquipmentProductionService
    {
        Task CreateDailyEquipment(string targetEquipmentId, DateOnly today, int goodQty, int badQty);
    }
}

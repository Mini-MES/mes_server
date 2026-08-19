using mes_server.Models.Analytics;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Services.EquipmentService
{
    public class DailyEquipmentProductionService : IDailyEquipmentProductionService
    {
        private readonly IGenericRepository<DailyEquipmentProduction> _dailyEquipmentProductionRepository;

        public DailyEquipmentProductionService(IGenericRepository<DailyEquipmentProduction> dailyEquipmentProductionRepository)
        {
            _dailyEquipmentProductionRepository = dailyEquipmentProductionRepository;
        }

        public async Task CreateDailyEquipmentProductionAsync(string targetEquipmentId, DateOnly today, int goodQty, int badQty, bool autoSave = true)
        {
            var daily = await _dailyEquipmentProductionRepository.FindAsync(d => d.EquipmentID == targetEquipmentId && d.WorkDate == today);

            if (daily == null)
            {
                daily = new DailyEquipmentProduction
                {
                    EquipmentID = targetEquipmentId,
                    WorkDate = today,
                    PlannedProductionMinutes = 960,
                    OperatingMinutes = 0,
                    DowntimeMinutes = 0,
                    TotalProducedQty = goodQty + badQty,
                    GoodQty = goodQty,
                    DefectQty = badQty,
                    IdealCycleTimeMinutes = 0.5m
                };
                await _dailyEquipmentProductionRepository.CreateAsync(daily);
            }
            else
            {
                daily.GoodQty += goodQty;
                daily.DefectQty += badQty;
                daily.TotalProducedQty += (goodQty + badQty);
            }

            if (autoSave)
            {
                await _dailyEquipmentProductionRepository.SaveChangesAsync();
            }
        }
    }
}

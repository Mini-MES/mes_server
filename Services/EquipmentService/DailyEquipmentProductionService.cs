using mes_server.Models.Analytics;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Services.EquipmentService
{
    public class DailyEquipmentProductionService : IDailyEquipmentProductionService
    {
        private readonly IGenericRepository<DailyEquipmentProduction> _dailyEquipmentProductionRepository;
        private readonly IGenericRepository<Equipment> _equipmentRepository;

        public DailyEquipmentProductionService(IGenericRepository<DailyEquipmentProduction> dailyEquipmentProductionRepository, IGenericRepository<Equipment> equipmentRepository)
        {
            _dailyEquipmentProductionRepository = dailyEquipmentProductionRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task CreateDailyEquipmentProductionAsync(string targetEquipmentId, DateOnly today, int goodQty, int badQty)
        {
            var daily = await _dailyEquipmentProductionRepository.FindAsync(d => d.EquipmentID == targetEquipmentId && d.WorkDate == today);

            var eq = await _equipmentRepository.FindAsync(e => e.EquipmentID == targetEquipmentId);

            int runningMin = (eq != null) ? (int)(eq.TotalRunningSeconds / 60) : 0;
            int downMin = (eq != null) ? (int)(eq.TotalDowntimeSeconds / 60) : 0;

            if (daily == null)
            {
                daily = new DailyEquipmentProduction
                {
                    EquipmentID = targetEquipmentId,
                    WorkDate = today,
                    PlannedProductionMinutes = 960,
                    OperatingMinutes = Math.Max(1, runningMin),
                    DowntimeMinutes = downMin,
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
                daily.OperatingMinutes = Math.Max(daily.OperatingMinutes, runningMin);
                daily.DowntimeMinutes = downMin;
            }

            await _dailyEquipmentProductionRepository.SaveChangesAsync();
        }
    }
}

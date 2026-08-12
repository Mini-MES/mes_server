using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.DTOs.Analytics;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IGenericRepository<Equipment> _equipmentRepository;
        private readonly IGenericRepository<DowntimeReasonMaster> _downtimeReasonRepository;
        private readonly MESDbContext _context;
        private readonly IHubContext<MesHub> _hubContext;

        public EquipmentService(IGenericRepository<Equipment> equipmentRepository, IGenericRepository<DowntimeReasonMaster> downtimeReasonRepository, MESDbContext context, IHubContext<MesHub> hubContext)
        {
            _equipmentRepository = equipmentRepository;
            _downtimeReasonRepository = downtimeReasonRepository;
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<bool> ChangeEquipmentStatusAsync(ChangeEquipmentStatusRequest request)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentID);
            if (equipment == null) return false;

            var oldStatus = equipment.Status;
            var newStatus = request.NewStatus;
            var now = DateTime.UtcNow;

            if (oldStatus == newStatus) return true;

            // RUNNING이 아니면 전부 비가동(Downtime) 상태로 간주
            bool isOldDowntime = oldStatus != EquipmentStatus.Running;
            bool isNewDowntime = newStatus != EquipmentStatus.Running;

            // (가동) ➔ (비가동) 으로 전환될 때만 1회 새로운 DowntimeLog 생성
            if (!isOldDowntime && isNewDowntime)
            {
                var downtimeLog = new DowntimeLog
                {
                    EquipmentID = equipment.EquipmentID,
                    StartedAt = now
                };
                _context.DowntimeLogs.Add(downtimeLog);
            }

            // (비가동) ➔ (가동 RUNNING) 으로 전환될 때만 열려있는 DowntimeLog 마감
            if (isOldDowntime && !isNewDowntime)
            {
                var openLog = await _context.DowntimeLogs
                    .Where(d => d.EquipmentID == equipment.EquipmentID && d.EndedAt == null)
                    .OrderByDescending(d => d.StartedAt)
                    .FirstOrDefaultAsync();

                if (openLog != null)
                {
                    openLog.EndedAt = now;
                    var durationSec = (int)(now - openLog.StartedAt).TotalSeconds;
                    openLog.DurationSeconds = durationSec;

                    equipment.TotalDowntimeSeconds += durationSec;
                }
            }

            equipment.Status = newStatus;
            if (!string.IsNullOrEmpty(request.CurrentLotID))
            {
                equipment.CurrentLotId = request.CurrentLotID;
            }
            else if (newStatus == EquipmentStatus.Running && string.IsNullOrEmpty(equipment.CurrentLotId))
            {
                var activeLot = await _context.Lots.FirstOrDefaultAsync(l => l.Status == mes_server.Models.Enum.LotStatus.WIP || l.Status == mes_server.Models.Enum.LotStatus.RELEASED);
                if (activeLot != null)
                {
                    equipment.CurrentLotId = activeLot.LotID;
                }
            }
            equipment.LastStatusChangedAt = now;

            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveEquipmentStatusChanged", new EquipmentDto
                {
                    EquipmentID = equipment.EquipmentID,
                    EquipmentName = equipment.Name,
                    Status = equipment.Status,
                    CurrentLotID = equipment.CurrentLotId,
                    TotalRunningSeconds = equipment.TotalRunningSeconds,
                    TotalDowntimeSeconds = equipment.TotalDowntimeSeconds,
                    LastStatusChangedAt = equipment.LastStatusChangedAt
                });
            }
            catch
            {
                
            }

            return true;
        }

        public async Task<IEnumerable<EquipmentDto>> GetAllEquipmentAsync()
        {
            var list = await _equipmentRepository.GetAllAsync();
            return list.Select(e => new EquipmentDto
            {
                EquipmentID = e.EquipmentID,
                EquipmentName = e.Name,
                Status = e.Status,
                CurrentLotID = e.CurrentLotId,
                TotalRunningSeconds = e.TotalRunningSeconds,
                TotalDowntimeSeconds = e.TotalDowntimeSeconds,
                LastStatusChangedAt = e.LastStatusChangedAt
            });
        }

        public async Task<IEnumerable<DowntimeLog>> GetDowntimeLogsByEquipmentAsync(string equipmentId)
        {
            return await _context.DowntimeLogs
                .Include(dl => dl.DowntimeReason)
                .Include(dl => dl.User)
                .Where(dl => dl.EquipmentID == equipmentId)
                .OrderByDescending(dl => dl.StartedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DowntimeReasonDto>> GetDowntimeReasonsAsync()
        {
            var reasons = await _downtimeReasonRepository.FindAsync(r => r.IsActive);
            return reasons.Select(r => new DowntimeReasonDto
            {
                ReasonCode = r.ReasonCode,
                ReasonName = r.ReasonName,
                Category = r.Category
            });
        }

        public async Task<EquipmentDto?> GetEquipmentByIdAsync(string equipmentId)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
            if (equipment == null)
            {
                throw new Exception($"Equipment with ID {equipmentId} not found.");
            }

            return new EquipmentDto
            {
                EquipmentID = equipment.EquipmentID,
                EquipmentName = equipment.Name,
                Status = equipment.Status,
                CurrentLotID = equipment.CurrentLotId,
                TotalRunningSeconds = equipment.TotalRunningSeconds,
                TotalDowntimeSeconds = equipment.TotalDowntimeSeconds,
                LastStatusChangedAt = equipment.LastStatusChangedAt,
            };
        }

        public async Task<bool> RegisterDowntimeReasonAsync(RegisterDowntimeReasonRequest request)
        {
            var logs = await _context.DowntimeLogs.FindAsync(request.DowntimeLogID);
            if (logs == null) return false;

            logs.ReasonCode = request.ReasonCode;
            logs.OperatorMemo = request.OperatorMemo;
            logs.UserID = request.UserID;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OEESummaryDto> GetOEESummaryAsync()
        {
            var equipments = await _context.Equipments.ToListAsync();
            var performances = await _context.Performances.ToListAsync();
            var lots = await _context.Lots.ToListAsync();
            var workOrders = await _context.WorkOrders.ToListAsync();

            var eqOeeList = new List<EquipmentOeeDto>();

            foreach (var eq in equipments)
            {
                // 해당 설비 연관 LOT 및 실적 데이터 필터링                                                                                                               
                var eqLotIds = lots
                    .Where(l => eq.CurrentLotId == l.LotID || l.LotID.Contains(eq.EquipmentID))
                    .Select(l => l.LotID)
                    .ToList();

                var eqPerformances = performances
                    .Where(p => eqLotIds.Contains(p.LotID))
                    .ToList();

                int eqGood = eqPerformances.Sum(p => p.GoodQty);
                int eqDefect = eqPerformances.Sum(p => p.BadQty);
                int eqTotalProd = eqGood + eqDefect;
                
                var activeWorkOrder = workOrders.FirstOrDefault(w => w.Status == OrderStatus.InProgress);
                int eqTarget = activeWorkOrder?.TargetQty ?? 100;
                                                                                                                                 
                double runSec = eq.TotalRunningSeconds;
                double downSec = eq.TotalDowntimeSeconds;
                double totalSec = runSec + downSec;
                                                                                                                                         
                double availability = totalSec > 0
                    ? Math.Min(100.0, Math.Round((runSec / totalSec) * 100.0, 1))
                    : 0.0;
                                                                                                                                    
                double runMin = runSec / 60.0;
                double performanceRate = runMin > 0
                    ? Math.Min(100.0, Math.Round(((0.8 * eqTotalProd) / runMin) * 100.0, 1))
                    : (eqTarget > 0 ? Math.Min(100.0, Math.Round(((double)eqTotalProd / eqTarget) * 100.0, 1)) : 0.0);
                                                                                                                                      
                double quality = eqTotalProd > 0
                    ? Math.Round(((double)eqGood / eqTotalProd) * 100.0, 1)
                    : 100.0;
                
                double oee = Math.Round((availability * performanceRate * quality) / 10000.0, 1);

                eqOeeList.Add(new EquipmentOeeDto
                {
                    EquipmentID = eq.EquipmentID,
                    EquipmentName = eq.Name,
                    Status = eq.Status,
                    TotalRunningMinutes = Math.Round(runSec / 60.0, 1),
                    TotalDowntimeMinutes = Math.Round(downSec / 60.0, 1),
                    TargetQty = eqTarget,
                    TotalProducedQty = eqTotalProd,
                    GoodQty = eqGood,
                    DefectQty = eqDefect,
                    AvailabilityRate = availability,
                    PerformanceRate = performanceRate,
                    QualityRate = quality,
                    OeePercentage = oee
                });
            }

            // 공장 전체 평균 계산                                                                                                                                        
            double avgOee = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.OeePercentage), 1) : 0.0;
            double avgAvail = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.AvailabilityRate), 1) : 0.0;
            double avgPerf = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.PerformanceRate), 1) : 0.0;
            double avgQual = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.QualityRate), 1) : 0.0;

            return new OEESummaryDto
            {
                OverallOee = avgOee,
                AverageAvailability = avgAvail,
                AveragePerformance = avgPerf,
                AverageQuality = avgQual,
                TotalEquipments = equipments.Count,
                RunningEquipments = equipments.Count(e => e.Status == EquipmentStatus.Running),
                StoppedEquipments = equipments.Count(e => e.Status != EquipmentStatus.Running),
                Equipments = eqOeeList
            };
        }

        public async Task<IEnumerable<DailyEquipmentProductionDto>> GetDailyEquipmentProductionsAsync(string? equipmentId = null, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            var query = _context.DailyEquipmentProductions
                .Include(d => d.Equipment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(equipmentId))
            {
                query = query.Where(d => d.EquipmentID == equipmentId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(d => d.WorkDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.WorkDate <= endDate.Value);
            }

            var list = await query
                .OrderByDescending(d => d.WorkDate)
                .ThenBy(d => d.EquipmentID)
                .ToListAsync();

            return list.Select(d => new DailyEquipmentProductionDto
            {
                DailyEquipmentOeeID = d.DailyEquipmentOeeID,
                WorkDate = d.WorkDate,
                EquipmentID = d.EquipmentID,
                EquipmentName = d.Equipment?.Name ?? d.EquipmentID,
                PlannedProductionMinutes = d.PlannedProductionMinutes,
                OperatingMinutes = d.OperatingMinutes,
                DowntimeMinutes = d.DowntimeMinutes,
                TotalProducedQty = d.TotalProducedQty,
                GoodQty = d.GoodQty,
                DefectQty = d.DefectQty,
                IdealCycleTimeMinutes = d.IdealCycleTimeMinutes,
                AvailabilityRate = d.AvailabilityRate,
                PerformanceRate = d.PerformanceRate,
                QualityRate = d.QualityRate,
                OeePercentage = d.OeePercentage
            });
        }
    }
}

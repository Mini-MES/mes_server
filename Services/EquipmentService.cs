using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.DTOs.MasterData;
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
            if(logs == null) return false;

            logs.ReasonCode = request.ReasonCode;
            logs.OperatorMemo = request.OperatorMemo;
            logs.UserID = request.UserID;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OEESummaryDto> GetOEESummaryAsync()
        {
            var equipments = await _equipmentRepository.GetAllAsync();
            var performance = await _context.Performances.ToListAsync();
            var workOrders = await _context.WorkOrders.ToListAsync();

            var eqOeeList = new List<EquipmentOeeDto>();

            int totalGood = performance.Sum(p => p.GoodQty);
            int totalDefect = performance.Sum(p => p.BadQty);
            int totalTarget = workOrders.Sum(w => w.TargetQty);

            foreach(var eq in equipments)
            {
                double runSec = eq.TotalRunningSeconds;
                double downSec = eq.TotalDowntimeSeconds;
                double totalSec = runSec + downSec;

                double availability = totalSec > 0 ? runSec / totalSec : 0;
                double performanceRate = totalTarget > 0 ? (double)(totalGood + totalDefect) / totalTarget : 0;
                double quality = (totalGood + totalDefect) > 0 ? (double)totalGood / (totalGood + totalDefect) : 0;

                double oee = availability * performanceRate * quality;
                
                eqOeeList.Add(new EquipmentOeeDto
                {
                    EquipmentId = eq.EquipmentID,
                    EquipmentName = eq.Name,
                    Status = eq.Status,
                    TotalRunningMinutes = runSec / 60,
                    TotalDowntimeMinutes = downSec / 60,
                    AvailabilityRate = availability * 100,
                    PerformanceRate = performanceRate * 100,
                    QualityRate = quality * 100,
                    OEE = oee * 100
                });
            }
            double avgOee = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.OEE), 1) : 0.0;
            double avgAvail = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.AvailabilityRate), 1) : 0.0;
            double avgPerf = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.PerformanceRate), 1) : 0.0;
            double avgQual = eqOeeList.Count > 0 ? Math.Round(eqOeeList.Average(e => e.QualityRate), 1) : 0.0;

            return new OEESummaryDto
            {
                AverageOEE = avgOee,
                AverageAvailabilityRate = avgAvail,
                AveragePerformanceRate = avgPerf,
                AverageQualityRate = avgQual,
                TotalEquipmentCount = equipments.Count(),
                RunningEquipmentCount = equipments.Count(e => e.Status == EquipmentStatus.Running),
                StoppedEquipmentCount = equipments.Count(e => e.Status != EquipmentStatus.Running),
            };
        }
    }
}

using mes_server.Models.DTOs.Analytics;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;

namespace mes_server.Services.EquipmentService
{
    public interface IEquipmentService
    {
        Task<IEnumerable<EquipmentDto>> GetAllEquipmentAsync();
        Task<EquipmentDto?> GetEquipmentByIdAsync(string equipmentId);
        Task<bool> ChangeEquipmentStatusAsync(ChangeEquipmentStatusRequest request, bool autoSave = true);
        Task<bool> RegisterDowntimeReasonAsync(RegisterDowntimeReasonRequest request);
        Task<IEnumerable<DowntimeReasonDto>> GetDowntimeReasonsAsync();
        Task<IEnumerable<DowntimeLog>> GetDowntimeLogsByEquipmentAsync(string equipmentId);
        Task<OEESummaryDto> GetOEESummaryAsync();
        Task<IEnumerable<DailyEquipmentProductionDto>> GetDailyEquipmentProductionsAsync(string? equipmentId = null, DateOnly? startDate = null, DateOnly? endDate = null);
        Task BroadcastTelemetryAsync(string equipmentId, double temperature, DateTime timestamp);
        Task AddRunningTimeAsync(string equipmentId, int seconds = 3, bool autoSave = true);
    }
}

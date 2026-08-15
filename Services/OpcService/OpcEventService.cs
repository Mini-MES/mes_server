using mes_server.Models.DTOs.MasterData;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Services.EquipmentService;
using mes_server.Services.ProductionService;

namespace mes_server.Services.OpcService
{
    public class OpcEventService : IOpcEventService
    {
        private readonly IPerformanceService _performanceService;
        private readonly IEquipmentService _equipmentService;
        private readonly ILogger<OpcEventService> _logger;

        private const string DemoEquipmentId = "CNC01";

        public OpcEventService(
            IPerformanceService performanceService,
            IEquipmentService equipmentService,
            ILogger<OpcEventService> logger)
        {
            _performanceService = performanceService;
            _equipmentService = equipmentService;
            _logger = logger;
        }

        public async Task HandleTagChangedAsync(string tagName, object value, DateTime timestamp)
        {
            switch (tagName)
            {
                case "Counter":
                    await HandleCounterAsync(timestamp);
                    break;

                case "Sinusoid":
                    await HandleTemperatureAsync(value, timestamp);
                    break;

                case "Square":
                    await HandleEquipmentStatusAsync(value, timestamp);
                    break;
            }
        }

        private async Task HandleCounterAsync(DateTime timestamp)
        {
            try
            {
                await _performanceService.RecordAutoProductionAsync(DemoEquipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [OpcEventService] Counter 이벤트 처리 중 오류 발생");
            }
        }

        private async Task HandleTemperatureAsync(object value, DateTime timestamp)
        {
            try
            {
                if (double.TryParse(value?.ToString(), out double sVal))
                {
                    await _equipmentService.BroadcastTelemetryAsync(sVal, timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [OpcEventService] Sinusoid 온도 처리 중 오류 발생");
            }
        }

        private async Task HandleEquipmentStatusAsync(object value, DateTime timestamp)
        {
            try
            {
                var isRunning = false;
                if (value is bool bVal)
                {
                    isRunning = bVal;
                }
                else if (int.TryParse(value?.ToString(), out int iVal))
                {
                    isRunning = iVal > 0;
                }
                else if (double.TryParse(value?.ToString(), out double dVal))
                {
                    isRunning = dVal > 0;
                }

                var status = isRunning ? EquipmentStatus.Running : EquipmentStatus.Idle;

                await _equipmentService.ChangeEquipmentStatusAsync(new ChangeEquipmentStatusRequest
                {
                    EquipmentID = DemoEquipmentId,
                    NewStatus = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [OpcEventService] Square 설비 상태 처리 중 오류 발생");
            }
        }
    }
}

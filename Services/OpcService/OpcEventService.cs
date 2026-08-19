using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;
using mes_server.Services.EquipmentService;
using mes_server.Services.ProductionService;
using System.Globalization;

namespace mes_server.Services.OpcService
{
    public class OpcEventService : IOpcEventService
    {
        private readonly IPerformanceService _performanceService;
        private readonly IEquipmentService _equipmentService;
        private readonly ILogger<OpcEventService> _logger;

        public OpcEventService(
            IPerformanceService performanceService,
            IEquipmentService equipmentService,
            ILogger<OpcEventService> logger)
        {
            _performanceService = performanceService;
            _equipmentService = equipmentService;
            _logger = logger;
        }

        public async Task HandleTagChangedAsync(OpcUaTagEvent tagEvent, long counterDelta = 0)
        {
            switch (tagEvent.TagType)
            {
                case OpcUaTagType.Counter:
                    await HandleCounterAsync(tagEvent, counterDelta);
                    break;

                case OpcUaTagType.Temperature:
                    await HandleTemperatureAsync(tagEvent);
                    break;

                case OpcUaTagType.Running:
                    await HandleEquipmentStatusAsync(tagEvent);
                    break;

                default:
                    throw new InvalidOperationException($"지원하지 않는 OPC UA TagType입니다: {tagEvent.TagType}");
            }

        }

        private async Task HandleCounterAsync(OpcUaTagEvent opcUaTagEvent, long counterDelta)
        {
            if(counterDelta <= 0)
            {
                return;
            }

            if (counterDelta > int.MaxValue)
            {
                throw new OverflowException($"Counter 증가량이 허용 범위를 초과했습니다: Equipment={opcUaTagEvent.EquipmentId}, Delta={counterDelta}");
            }

            var performance = await _performanceService.RecordAutoProductionAsync(opcUaTagEvent.EquipmentId, (int)counterDelta);

            if (performance == null)
            {
                _logger.LogDebug("OPC Counter 실적 등록 생략: Equipment={EquipmentId}, Delta={Delta}", opcUaTagEvent.EquipmentId, counterDelta);

                return;
            }

            _logger.LogInformation(
                "OPC Counter 실적 전달 완료: Equipment={EquipmentId}, Delta={Delta}", opcUaTagEvent.EquipmentId, counterDelta);
        }

        private async Task HandleTemperatureAsync(OpcUaTagEvent opcUaTagEvent)
        {
            
            try
            {
                double temperature = Convert.ToDouble(opcUaTagEvent.Value, CultureInfo.InvariantCulture);
                await _equipmentService.BroadcastTelemetryAsync(opcUaTagEvent.EquipmentId, temperature, opcUaTagEvent.Timestamp); ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [OpcEventService] 온도 처리 중 오류 발생");
            }
        }

        private async Task HandleEquipmentStatusAsync(OpcUaTagEvent opcUaTagEvent)
        {
            try
            {
                bool isRunning;
                if (opcUaTagEvent.Value is bool booleanValue)
                {
                    isRunning = booleanValue;
                }
                else if (!bool.TryParse(opcUaTagEvent.Value?.ToString(), out isRunning))
                {
                    throw new InvalidCastException($"Running 값을 Boolean으로 변환할 수 없습니다: Equipment={opcUaTagEvent.EquipmentId}, Value={opcUaTagEvent.Value}");
                }

                var status = isRunning ? EquipmentStatus.Running : EquipmentStatus.Idle;
                var changed = await _equipmentService.ChangeEquipmentStatusAsync(new ChangeEquipmentStatusRequest
                {
                    EquipmentID = opcUaTagEvent.EquipmentId,
                    NewStatus = status
                });

                if (!changed)
                {
                    throw new KeyNotFoundException(
                        $"OPC 상태 변경 대상 설비를 찾을 수 없습니다: {opcUaTagEvent.EquipmentId}");
                }

                _logger.LogInformation("OPC 설비 상태 반영: Equipment={EquipmentId}, Running={Running}, Status={Status}", opcUaTagEvent.EquipmentId, isRunning, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [OpcEventService] Square 설비 상태 처리 중 오류 발생");
            }
        }
    }
}

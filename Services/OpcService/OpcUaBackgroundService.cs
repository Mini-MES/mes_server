using mes_server.Models.MasterData;
using System.Globalization;
namespace mes_server.Services.OpcService
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OpcUaBackgroundService> _logger;

        private readonly SemaphoreSlim _counterLock = new(1, 1);
        private readonly Dictionary<string, long> _lastCounters = new(StringComparer.OrdinalIgnoreCase);

        private CancellationToken _stoppingToken;

        public OpcUaBackgroundService(
            IOpcUaService opcUaService,
            IServiceScopeFactory scopeFactory,
            ILogger<OpcUaBackgroundService> logger)
        {
            _opcUaService = opcUaService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            _logger.LogInformation("🚀 [OPC UA Pulse 수집 서비스] 실시간 생산 연동 가동");

            _opcUaService.OnDataReceived += HandleDataReceived;

            try
            {
                await _opcUaService.ConnectAndSubscribeAsync();
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 애플리케이션 정상 종료
            }
            finally
            {
                _opcUaService.OnDataReceived -= HandleDataReceived;
                await _opcUaService.DisconnectAsync();
            }
        }

        private async void HandleDataReceived(OpcUaTagEvent tagEvent)
        {
            if (_stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (tagEvent.TagType == OpcUaTagType.Counter)
                {
                    await HandleCounterAsync(tagEvent);
                    return;
                }

                if (tagEvent.TagType == OpcUaTagType.Running)
                {
                    _logger.LogInformation(
                        "[OPC Running 수신] Equipment={EquipmentId}, Value={Value}, Timestamp={Timestamp:O}",
                        tagEvent.EquipmentId,
                        tagEvent.Value,
                        tagEvent.Timestamp);
                }
                await DispatchEventAsync(tagEvent, counterDelta: 0);
            }
            catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
            {
                // 애플리케이션 정상 종료
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "⚠️ OPC UA 이벤트 처리 실패: Equipment={EquipmentId}, Tag={TagType}, Value={Value}",
                    tagEvent.EquipmentId,
                    tagEvent.TagType,
                    tagEvent.Value);
            }
        }

        private async Task HandleCounterAsync(OpcUaTagEvent tagEvent)
        {
            long currentCounter;

            try
            {
                currentCounter = Convert.ToInt64(tagEvent.Value, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Counter 값을 정수로 변환할 수 없습니다: Equipment={EquipmentId}, Value={Value}",
                    tagEvent.EquipmentId,
                    tagEvent.Value);

                return;
            }

            await _counterLock.WaitAsync(_stoppingToken);

            try
            {
                if (!_lastCounters.TryGetValue(tagEvent.EquipmentId, out var previousCounter))
                {
                    _lastCounters[tagEvent.EquipmentId] = currentCounter;

                    _logger.LogInformation(
                        "OPC Counter 초기 기준값 설정: Equipment={EquipmentId}, Counter={Counter}",
                        tagEvent.EquipmentId,
                        currentCounter);

                    return;
                }

                if (currentCounter == previousCounter)
                {
                    return;
                }

                if (currentCounter < previousCounter)
                {
                    _lastCounters[tagEvent.EquipmentId] = currentCounter;

                    _logger.LogInformation(
                        "OPC Counter Reset 감지: Equipment={EquipmentId}, Previous={Previous}, Current={Current}",
                        tagEvent.EquipmentId,
                        previousCounter,
                        currentCounter);

                    return;
                }

                var counterDelta = currentCounter - previousCounter;

                await DispatchEventAsync(tagEvent, counterDelta);

                // 이벤트 처리가 성공했을 때만 기준값을 갱신한다.
                _lastCounters[tagEvent.EquipmentId] = currentCounter;
                _logger.LogInformation(
                    "OPC Counter 증가 처리: Equipment={EquipmentId}, Previous={Previous}, Current={Current}, Delta={Delta}",
                    tagEvent.EquipmentId,
                    previousCounter,
                    currentCounter,
                    counterDelta);
            }
            finally
            {
                _counterLock.Release();
            }
        }

        private async Task DispatchEventAsync(OpcUaTagEvent tagEvent, long counterDelta)
        {
            using var scope = _scopeFactory.CreateScope();

            var opcEventService = scope.ServiceProvider.GetRequiredService<IOpcEventService>();

            await opcEventService.HandleTagChangedAsync(tagEvent, counterDelta);
        }

        public override void Dispose()
        {
            _counterLock.Dispose();
            base.Dispose();
        }
    }
}

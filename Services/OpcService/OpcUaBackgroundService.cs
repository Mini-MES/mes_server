namespace mes_server.Services.OpcService
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OpcUaBackgroundService> _logger;

        // Counter: 생산 펄스 동시성 제어 및 순차 처리 보장
        private readonly SemaphoreSlim _counterLock = new(1, 1);

        // Sinusoid: 이전 텔레메트리 전송 중일 경우 중복 방지 (Throttling)
        private readonly SemaphoreSlim _telemetryLock = new(1, 1);

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
            _logger.LogInformation("🚀 [OPC UA Pulse 수집 서비스] 실시간 생산 연동 가동");

            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                try
                {
                    switch (tagName)
                    {
                        case "Counter":
                            // 실적 카운트는 데이터 누락 없도록 락 획득 후 순차 실행
                            await _counterLock.WaitAsync(stoppingToken);
                            try
                            {
                                await DispatchEventAsync(tagName, value, timestamp);
                            }
                            finally
                            {
                                _counterLock.Release();
                            }
                            break;

                        case "Sinusoid":
                            // 텔레메트리는 이전 브로드캐스트가 진행 중이면 대기 없이 스킵
                            if (await _telemetryLock.WaitAsync(0, stoppingToken))
                            {
                                try
                                {
                                    await DispatchEventAsync(tagName, value, timestamp);
                                }
                                finally
                                {
                                    _telemetryLock.Release();
                                }
                            }
                            break;

                        default:
                            await DispatchEventAsync(tagName, value, timestamp);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 서비스 종료 시 취소 예외 무시
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ OPC UA 처리 중 오류 발생 (Tag: {TagName})", tagName);
                }
            };

            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

        private async Task DispatchEventAsync(string tagName, object value, DateTime timestamp)
        {
            using var scope = _scopeFactory.CreateScope();
            var opcEventService = scope.ServiceProvider.GetRequiredService<IOpcEventService>();
            await opcEventService.HandleTagChangedAsync(tagName, value, timestamp);
        }

        public override void Dispose()
        {
            _counterLock.Dispose();
            _telemetryLock.Dispose();
            base.Dispose();
        }
    }
}

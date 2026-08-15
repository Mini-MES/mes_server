using mes_server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Services.OpcService
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly ILogger<OpcUaBackgroundService> _logger;
        private readonly OpcEventService _opcEventService;

        public OpcUaBackgroundService(
            IOpcUaService opcUaService,
            ILogger<OpcUaBackgroundService> logger,
            OpcEventService opcEventService
            )
        {
            _opcUaService = opcUaService;
            _logger = logger;
            _opcEventService = opcEventService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [OPC UA Pulse 수집 서비스] 실시간 생산 연동 가동");

            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                try
                {
                    await _opcEventService.HandleTagChangedAsync(tagName, value, timestamp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ OPC UA 처리 중 오류 발생");
                }
            };

            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

    }
}

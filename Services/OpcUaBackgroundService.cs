using mes_server.Hubs;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Services
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<OpcUaBackgroundService> _logger;

        public OpcUaBackgroundService(
            IOpcUaService opcUaService,
            IHubContext<MesHub> hubContext,
            ILogger<OpcUaBackgroundService> logger)
        {
            _opcUaService = opcUaService;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 OpcUaBackgroundService 시작됨");

            // OPC UA 태그 데이터 수신 이벤트 발생 시 SignalR(MesHub)로 실시간 전달
            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveOpcData", new
                    {
                        TagName = tagName,
                        Value = value,
                        Timestamp = timestamp
                    }, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SignalR OPC UA 데이터 전송 중 오류 발생");
                }
            };

            // 데모 서버 연결 및 구독 시작
            await _opcUaService.ConnectAndSubscribeAsync();

            // 백그라운드 서비스 상주
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            // 서비스 종료 시 연결 해제
            await _opcUaService.DisconnectAsync();
        }
    }
}

using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class OpcUaBackgroundService : BackgroundService
    {
        private readonly IOpcUaService _opcUaService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<OpcUaBackgroundService> _logger;

        // MES 텔레메트리 최신 상태 데이터 (온도, 상태, 누적 수량)
        private double _currentTemperature = 65.0;
        private string _currentStatus = EquipmentStatus.Running;
        private int _totalCount = 0;

        public OpcUaBackgroundService(
            IOpcUaService opcUaService,
            IServiceScopeFactory scopeFactory,
            IHubContext<MesHub> hubContext,
            ILogger<OpcUaBackgroundService> logger)
        {
            _opcUaService = opcUaService;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [OPC UA 백그라운드 MES 연동 서비스] 가동 시작");

            // OPC UA 수신 ➔ MES 변환 ➔ DB & SignalR 브로드캐스트
            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                try
                {
                    bool isDataUpdated = false;

                    switch (tagName)
                    {
                        // 1. Sinusoid ➔ 설비 온도 (65°C ± 15°C 변동)
                        case "Sinusoid":
                            if (double.TryParse(value?.ToString(), out double sinVal))
                            {
                                _currentTemperature = Math.Round(65.0 + (sinVal * 15.0), 1);
                                isDataUpdated = true;
                            }
                            break;

                        // 2. Square ➔ 설비 가동/대기 상태 (1: RUNNING, 0: IDLE)
                        case "Square":
                            if (int.TryParse(value?.ToString(), out int sqVal))
                            {
                                string newStatus = sqVal == 1 ? EquipmentStatus.Running : EquipmentStatus.Idle;
                                if (_currentStatus != newStatus)
                                {
                                    _currentStatus = newStatus;
                                    await UpdateEquipmentStatusInDbAsync(_currentStatus, stoppingToken);
                                }
                                isDataUpdated = true;
                            }
                            break;

                        // 3. Counter ➔ 누적 생산 수량
                        case "Counter":
                            if (int.TryParse(value?.ToString(), out int cntVal))
                            {
                                _totalCount = cntVal;
                                isDataUpdated = true;
                            }
                            break;
                    }

                    // 변동 발생 시 SignalR(MesHub)로 실시간 MES 텔레메트리 발송
                    if (isDataUpdated)
                    {
                        var telemetryPayload = new
                        {
                            EquipmentId = "EQ-001",
                            Temperature = _currentTemperature,
                            Status = _currentStatus,
                            TotalCount = _totalCount,
                            Timestamp = timestamp
                        };

                        await _hubContext.Clients.All.SendAsync("ReceiveEquipmentTelemetry", telemetryPayload, stoppingToken);
                        _logger.LogInformation("📡 [MES 텔레메트리 전송] EQ-001 | 온도: {Temp}°C | 상태: {Status} | 생산수량: {Count}개",
                            _currentTemperature, _currentStatus, _totalCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ OPC UA 수신 데이터 처리 중 오류 발생");
                }
            };

            // 연결 및 구독 시작
            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

        // DB 내 첫 번째 설비 상태 자동 갱신
        private async Task UpdateEquipmentStatusInDbAsync(string newStatus, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                var equipment = await dbContext.Equipments.FirstOrDefaultAsync(ct);
                if (equipment != null)
                {
                    equipment.Status = newStatus;
                    equipment.LastStatusChangedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("💾 [DB 갱신 완료] 설비 [{EqId}] 상태 변경 -> {Status}", equipment.EquipmentID, newStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ DB 설비 상태 업데이트 실패");
            }
        }
    }
}

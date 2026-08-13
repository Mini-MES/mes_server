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

        // 원본 OPC UA 변수 값 캐시 (Sinusoid, Square, Counter)
        private double _latestSinusoid = 0.0;
        private int _latestSquare = 1;
        private int _latestCounter = 0;

        // CNC01 ~ CNC05 설비별 특성 오프셋 설정 (온도 가공, 생산 속도, 상태 차별화)
        private readonly (string Id, double BaseTemp, double TempMulti, double CountMulti, string? FixedStatus)[] _cncProfiles = new[]
        {
            ("CNC01", 65.0, 15.0, 1.0,  (string?)null),                // CNC 선반 #1 (메인 가동)
            ("CNC02", 62.0, 13.0, 0.95, (string?)null),                // CNC 선반 #2
            ("CNC03", 58.0, 10.0, 0.0,  EquipmentStatus.Stopped),      // CNC 밀링 #1 (정지/정비 중)
            ("CNC04", 67.0, 16.0, 1.05, (string?)null),                // CNC 밀링 #2 (고속 가동)
            ("CNC05", 55.0, 8.0,  0.8,  (string?)null)                 // 연삭기
        };

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
            _logger.LogInformation("🚀 [5대 핵심 설비 CNC01~CNC05 OPC UA 연동 서비스] 가동 시작");

            _opcUaService.OnDataReceived += async (tagName, value, timestamp) =>
            {
                try
                {
                    bool isUpdated = false;

                    switch (tagName)
                    {
                        case "Sinusoid":
                            if (double.TryParse(value?.ToString(), out double sVal))
                            {
                                _latestSinusoid = sVal;
                                isUpdated = true;
                            }
                            break;

                        case "Square":
                            if (int.TryParse(value?.ToString(), out int qVal))
                            {
                                _latestSquare = qVal;
                                isUpdated = true;
                            }
                            break;

                        case "Counter":
                            if (int.TryParse(value?.ToString(), out int cVal))
                            {
                                _latestCounter = cVal;
                                isUpdated = true;
                            }
                            break;
                    }

                    // 수치 수신 시 CNC01 ~ CNC05 전체 5대 설비 텔레메트리 리스트 생성
                    if (isUpdated)
                    {
                        var telemetryList = new List<object>();
                        var runningStatusMap = new Dictionary<string, string>();

                        foreach (var profile in _cncProfiles)
                        {
                            double temp = Math.Round(profile.BaseTemp + (_latestSinusoid * profile.TempMulti), 1);
                            string status = profile.FixedStatus 
                                ?? (_latestSquare == 1 ? EquipmentStatus.Running : EquipmentStatus.Idle);
                            int count = (int)(_latestCounter * profile.CountMulti);

                            runningStatusMap[profile.Id] = status;

                            telemetryList.Add(new
                            {
                                EquipmentId = profile.Id,
                                Temperature = temp,
                                Status = status,
                                TotalCount = count,
                                Timestamp = timestamp
                            });
                        }

                        // DB 내 CNC01 ~ CNC05 설비 상태 동괄 업데이트
                        await UpdateAllEquipmentStatusesInDbAsync(runningStatusMap, stoppingToken);

                        // SignalR로 5대 설비 전체 텔레메트리 발송
                        await _hubContext.Clients.All.SendAsync("ReceiveEquipmentTelemetryList", telemetryList, stoppingToken);
                        
                        _logger.LogInformation("📡 [5대 설비 전체 텔레메트리 전송] CNC01~CNC05 데이터 브로드캐스트 완료");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ CNC01~CNC05 텔레메트리 처리 중 오류 발생");
                }
            };

            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

        // DB 내 CNC01 ~ CNC05 5대 설비 상태 동괄 업데이트
        private async Task UpdateAllEquipmentStatusesInDbAsync(Dictionary<string, string> statusMap, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                var equipments = await dbContext.Equipments.ToListAsync(ct);
                bool isChanged = false;

                foreach (var eq in equipments)
                {
                    if (statusMap.TryGetValue(eq.EquipmentID, out string? newStatus) && eq.Status != newStatus)
                    {
                        eq.Status = newStatus;
                        eq.LastStatusChangedAt = DateTime.UtcNow;
                        isChanged = true;
                    }
                }

                if (isChanged)
                {
                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("💾 [DB 동괄 갱신 완료] CNC01~CNC05 설비 상태 업데이트 저장 성공");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ DB 설비 상태 전체 동괄 업데이트 실패");
            }
        }
    }
}

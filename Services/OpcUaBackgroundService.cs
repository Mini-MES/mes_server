using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.History;
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

        private double _latestSinusoid = 0.0;
        private int _latestCounter = 0;
        private int _lastProcessedCounter = -1;

        // CNC01 ~ CNC05 설비 및 공정 ID 매핑 (ProcessID: 2=선삭, 3=밀링, 5=연삭)
        private readonly (string Id, int ProcessId, double BaseTemp, double TempMulti)[] _cncProfiles = new[]
        {
            ("CNC01", 2, 65.0, 15.0), // CNC 선삭 #1 (공정 2)
            ("CNC02", 2, 62.0, 13.0), // CNC 선삭 #2 (공정 2)
            ("CNC03", 3, 58.0, 10.0), // CNC 밀링 #1 (공정 3)
            ("CNC04", 3, 67.0, 16.0), // CNC 밀링 #2 (공정 3)
            ("CNC05", 5, 55.0, 8.0)   // 연삭기 (공정 5)
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
            _logger.LogInformation("🚀 [공정 흐름 연동형 스마트 OPC UA 서비스] 가동 시작");

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

                        case "Counter":
                            if (int.TryParse(value?.ToString(), out int cVal))
                            {
                                _latestCounter = cVal;
                                isUpdated = true;
                            }
                            break;
                    }

                    if (isUpdated)
                    {
                        await ProcessProcessRoutingAndTelemetryAsync(timestamp, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ 공정 연동 OPC UA 처리 중 오류 발생");
                }
            };

            await _opcUaService.ConnectAndSubscribeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }

            await _opcUaService.DisconnectAsync();
        }

        private async Task ProcessProcessRoutingAndTelemetryAsync(DateTime timestamp, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

            var dbEquipments = await dbContext.Equipments.ToDictionaryAsync(e => e.EquipmentID, ct);
            var activeLots = await dbContext.Lots
                .Where(l => l.Status == mes_server.Models.Enum.LotStatus.WIP || l.Status == mes_server.Models.Enum.LotStatus.RELEASED)
                .ToListAsync(ct);

            bool isCounterIncreased = (_latestCounter != _lastProcessedCounter && _lastProcessedCounter != -1);
            if (_lastProcessedCounter == -1) _lastProcessedCounter = _latestCounter;

            var telemetryList = new List<object>();

            foreach (var profile in _cncProfiles)
            {
                dbEquipments.TryGetValue(profile.Id, out var eq);
                string dbStatus = eq?.Status ?? EquipmentStatus.Running;

                // 1. 온도는 설비 가동 유무 상관없이 OPC UA 신호에 따라 실시간 쏴줌
                double temp = (dbStatus == EquipmentStatus.Running)
                    ? Math.Round(profile.BaseTemp + (_latestSinusoid * profile.TempMulti), 1)
                    : Math.Round(25.0 + (_latestSinusoid * 1.5), 1);

                // 2. 해당 설비 공정(ProcessId)을 현재 통과 중인 active LOT 조회
                var activeLotInProcess = activeLots.FirstOrDefault(l => l.CurrentProcessID == profile.ProcessId);

                // 3. Counter 펄스 증가 시 + 설비 가동(RUNNING) 중 + 해당 공정을 지나가는 LOT가 있을 때만 DB 양품 실적(+1) 생성
                if (isCounterIncreased && dbStatus == EquipmentStatus.Running && activeLotInProcess != null)
                {
                    var perf = new Performance
                    {
                        WorkOrderID = activeLotInProcess.OrderID,
                        LotID = activeLotInProcess.LotID,
                        ProcessID = profile.ProcessId,
                        UserID = "operator1",
                        InputQty = 1,
                        GoodQty = 1,
                        BadQty = 0,
                        WorkDate = DateTime.Now
                    };
                    dbContext.Performances.Add(perf);

                    if (eq != null)
                    {
                        eq.TotalRunningSeconds += 3;
                    }

                    _logger.LogInformation("✨ [양품 실적 적재] 설비 [{EqId}] (공정 {ProcessId}) ➔ LOT [{LotId}] 양품 1개 생산 완료!",
                        profile.Id, profile.ProcessId, activeLotInProcess.LotID);
                }

                // DB 상 해당 설비의 실제 양품 적재 수량 조회
                int totalProdQty = 0;
                if (eq != null)
                {
                    var eqLotIds = activeLots
                        .Where(l => eq.CurrentLotId == l.LotID || l.LotID.Contains(eq.EquipmentID))
                        .Select(l => l.LotID)
                        .ToList();

                    totalProdQty = await dbContext.Performances
                        .Where(p => eqLotIds.Contains(p.LotID))
                        .SumAsync(p => p.GoodQty, ct);
                }

                telemetryList.Add(new
                {
                    EquipmentId = profile.Id,
                    Temperature = temp,
                    Status = dbStatus,
                    TotalCount = totalProdQty,
                    Timestamp = timestamp
                });
            }

            if (isCounterIncreased)
            {
                _lastProcessedCounter = _latestCounter;
                await dbContext.SaveChangesAsync(ct);
            }

            // SignalR로 실시간 텔레메트리 송신
            await _hubContext.Clients.All.SendAsync("ReceiveEquipmentTelemetryList", telemetryList, ct);
        }
    }
}

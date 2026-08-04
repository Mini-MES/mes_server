using mes_server.Data;
using mes_server.Hubs;
using mes_server.Models.MasterData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class AutomatedSensorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<AutomatedSensorBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public AutomatedSensorBackgroundService(
            IServiceScopeFactory scopeFactory,
            IHubContext<MesHub> hubContext,
            ILogger<AutomatedSensorBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 현장 생산 지시 연동 센서 가동 서비스 시작됨");

            while (!stoppingToken.IsCancellationRequested)
            {
                bool isEnabled = _configuration.GetValue<bool>("SensorSimulation:Enabled", true);
                int intervalSeconds = _configuration.GetValue<int>("SensorSimulation:IntervalSeconds", 3);

                if (isEnabled)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();

                        // 💡 현재 상태가 'RUNNING'이고, 생산 중인 LOT(CurrentLotId)이 존재하는 설비만 조회                                                                                        
                        var runningEquipments = await dbContext.Equipments
                            .Where(e => e.Status == EquipmentStatus.Running && !string.IsNullOrEmpty(e.CurrentLotId))
                            .ToListAsync(stoppingToken);

                        foreach (var equipment in runningEquipments)
                        {
                            // 1. 설정된 주기(초)만큼 누적 가동 시간 증가                                                                                                                          
                            equipment.TotalRunningSeconds += intervalSeconds;
                            await dbContext.SaveChangesAsync(stoppingToken);

                            // 2. 📡 실시간 양품 +1 수량 카운트 펄스를 웹 화면으로 전송                                                                                                            
                            await _hubContext.Clients.All.SendAsync("ReceiveSensorCountUpdated", new
                            {
                                EquipmentID = equipment.EquipmentID,
                                LotID = equipment.CurrentLotId,
                                GoodIncrement = 1,
                                BadIncrement = 0,
                                Timestamp = DateTime.UtcNow
                            }, stoppingToken);

                            _logger.LogInformation("⚡ [센서 카운트 +1] 설비: {EqId}, Lot: {LotId}, 누적가동: {Sec}초",
                                equipment.EquipmentID, equipment.CurrentLotId, equipment.TotalRunningSeconds);
                        }
                    }
                }
                                                                                                                   
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}

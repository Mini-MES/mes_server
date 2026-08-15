using mes_server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Services.OpcService
{
    public class OpcEventService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MesHub> _hubContext;
        private readonly ILogger<OpcEventService> _logger;

        private readonly SemaphoreSlim _lock = new(1, 1);

        private double _latestSinusoid;

        private const string DemoEquipmentId = "CNC01";
        private const int DemoProcessId = 2;

        public OpcEventService(
            IServiceScopeFactory scopeFactory,
            IHubContext<MesHub> hubContext,
            ILogger<OpcEventService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleTagChangedAsync(
            string tagName,
            object value,
            DateTime timestamp)
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

        private Task HandleCounterAsync(DateTime timestamp)
        {
            return Task.CompletedTask;
        }

        private Task HandleTemperatureAsync(object value, DateTime timestamp)
        {
            return Task.CompletedTask;
        }

        private Task HandleEquipmentStatusAsync(object value, DateTime timestamp)
        {
           return Task.CompletedTask;
        }
    }
}

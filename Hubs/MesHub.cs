using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Hubs
{
    [Authorize] 
    public class MesHub : Hub
    {
        private readonly ILogger<MesHub> _logger;

        public MesHub(ILogger<MesHub> logger)
        {
            _logger = logger;
        }

        // SignalR 클라이언트 접속 시 호출되는 메서드
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"🟢 SignalR 클라이언트 접속됨: ConnectionId={Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // SignalR 클라이언트 접속 해제 시 호출되는 메서드
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"🔴 SignalR 클라이언트 접속 해제됨: ConnectionId={Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}

# 📡 ASP.NET Core 8 (`mes_server`) SignalR 백엔드 구축 가이드

본 문서는 **ASP.NET Core 8 Web API (`mes_server`)**에서 **SignalR**을 구축하여 프론트엔드(`mes_front`)로 실시간 이벤트를 전송하는 가이드입니다.

---

## 1. 📁 백엔드 권장 폴더 구조

```text
mes_server/
├── Hubs/
│   └── MesHub.cs            # ⚡ SignalR 허브 클래스
├── Controllers/
│   ├── ProductionController.cs # IHubContext<MesHub> 주입 및 이벤트 전송
│   ├── InventoryController.cs  # IHubContext<MesHub> 주입 및 이벤트 전송
│   └── MasterDataController.cs
├── Program.cs               # ⚙️ SignalR 서비스 및 CORS, Hub 엔드포인트 등록
└── mes_server.csproj
```

---

## 2. 🛠️ 단계별 구축 가이드

### 1단계: SignalR 허브 클래스 작성 (`Hubs/MesHub.cs`)

`Hubs` 폴더를 생성하고 `MesHub.cs` 클래스를 정의합니다. `Microsoft.AspNetCore.SignalR.Hub`를 상속받습니다.

```csharp
using Microsoft.AspNetCore.SignalR;

namespace mes_server.Hubs
{
    public class MesHub : Hub
    {
        private readonly ILogger<MesHub> _logger;

        public MesHub(ILogger<MesHub> logger)
        {
            _logger = logger;
        }

        // 클라이언트(프론트엔드)가 접속했을 때 자동 실행
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"🟢 SignalR 클라이언트 접속됨: ConnectionId={Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // 클라이언트 접속 해제 시 자동 실행
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogWarning($"🔴 SignalR 클라이언트 접속 해제: ConnectionId={Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
```

---

### 2단계: `Program.cs` 서비스 및 엔드포인트 설정

`Program.cs` 파일에 SignalR 서비스 등록, CORS 허용 설정(`AllowCredentials`), 그리고 Hub 라우팅을 추가합니다.

```csharp
using mes_server.Hubs; // Hubs 네임스페이스 추가

var builder = WebApplication.CreateBuilder(args);

// 1. SignalR 서비스 추가
builder.Services.AddSignalR();

// 2. CORS 정책 설정 (SignalR 소켓 통신을 위해 AllowCredentials() 필수!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // 프론트엔드 주소
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // ⚠️ SignalR 필수 항목!
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. CORS 미들웨어 적용
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

// 4. SignalR Hub 엔드포인트 라우팅 매핑
app.MapHub<MesHub>("/hubs/mes");

app.Run();
```

---

### 3단계: 컨트롤러 및 서비스에서 이벤트 전송 (`IHubContext<MesHub>`)

API 컨트롤러에서 데이터가 변경되었을 때, `IHubContext<MesHub>`를 의존성 주입(DI) 받아 프론트엔드로 이벤트를 전송합니다.

#### 예시 A: 생산 공정 이동 시 (`ProductionController.cs`)
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using mes_server.Hubs;

[ApiController]
[Route("api/[controller]")]
public class ProductionController : ControllerBase
{
    private readonly IHubContext<MesHub> _hub;

    public ProductionController(IHubContext<MesHub> hub)
    {
        _hub = hub;
    }

    [HttpPost("move-stage")]
    public async Task<IActionResult> MoveStage([FromBody] MoveStageRequest request)
    {
        // ... DB 공정 이동 트랜잭션 수행 ...

        // ⚡ 프론트엔드로 'LotUpdated' 실시간 이벤트 방송
        await _hub.Clients.All.SendAsync("LotUpdated", new 
        { 
            lotId = request.LotId, 
            updatedAt = DateTime.UtcNow 
        });

        return Ok(new { success = true });
    }
}
```

#### 예시 B: 원자재 입고 및 재고 수정 시 (`InventoryController.cs`)
```csharp
[HttpPost("update-stock")]
public async Task<IActionResult> UpdateStock([FromBody] UpdateStockRequest request)
{
    // ... DB 재고 업데이트 처리 ...

    // ⚡ 프론트엔드로 'StockUpdated' 실시간 이벤트 방송
    await _hub.Clients.All.SendAsync("StockUpdated", new 
    { 
        productId = request.ProductId, 
        newStock = request.NewQty 
    });

    return Ok(new { success = true });
}
```

#### 예시 C: 현장 불량 발생 시 (`DefectController.cs`)
```csharp
[HttpPost("report-defect")]
public async Task<IActionResult> ReportDefect([FromBody] DefectRequest request)
{
    // ... 불량 등록 및 LOT 상태 HOLD 보류 처리 ...

    // ⚡ 프론트엔드로 'DefectReported' 이벤트 방송
    await _hub.Clients.All.SendAsync("DefectReported", new 
    { 
        lotId = request.LotId, 
        reasonCode = request.ReasonCode 
    });

    return Ok(new { success = true });
}
```

---

## 3. 🎯 프론트엔드-백엔드 이벤트 이름 매핑 표

| 백엔드 발송 메서드명 (`SendAsync`) | 프론트엔드 수신 이벤트 | 프론트엔드 반응 (React Query) |
| :--- | :--- | :--- |
| **`"LotUpdated"`** | `connection.on('LotUpdated')` | `['lot-tracking']`, `['work-orders']` 쿼리 자동 갱신 |
| **`"StockUpdated"`** | `connection.on('StockUpdated')` | `['products']` 쿼리 자동 갱신 |
| **`"WorkOrderUpdated"`** | `connection.on('WorkOrderUpdated')` | `['work-orders']` 쿼리 자동 갱신 |
| **`"DefectReported"`** | `connection.on('DefectReported')` | `['lot-tracking']` 무효화 & HOLD 보류 알림 |

---

## 🧪 4. 검증 및 확인 방법

1. `mes_server` 백엔드 프로젝트 실행 (`dotnet run`)
2. 브라우저에서 프론트엔드(`http://localhost:5173`) 접속
3. 헤더 상단에 **`🟢 실시간 (SignalR)`** 배지가 뜨는지 확인
4. 백엔드 콘솔 로그에 `🟢 SignalR 클라이언트 접속됨: ConnectionId=...` 로그 확인

# 📌 [Issue #1] 백엔드 C# 설비 가동/비가동 데이터 모델링 & 실시간 센서 자동화 시뮬레이터 구축

- **이슈 ID**: `ISSUE-01`
- **라벨**: `Feat`, `Backend`, `C#`, `SignalR`, `EF Core`
- **관련 문서**: [`NEXT_STEP_FEATURE_EXPANSION.md`](../../mes_front/docs/NEXT_STEP_FEATURE_EXPANSION.md)

---

## 📝 1. 작업 개요
현장 PLC/센서 신호 연동 및 설비 상태(`RUNNING`, `STOPPED`, `MAINTENANCE`, `ERROR` 등) 관리를 위한 EF Core 데이터베이스 모델을 구축하고, 개발 및 웹 테스트를 위한 가상 센서 백그라운드 서비스를 구현합니다.

> 💡 **설계 핵심 방침**: 
> 설비 상태(`Status`)는 추후 현장에서 새로운 상태(예: `SETUP`, `INSPECTION`)가 추가되더라도 DB 스키마 파괴나 코드 재배포 시 기존 데이터 오염이 없도록 **C# 문자열 상수 클래스(`EquipmentStatus.cs`)** 및 DB `nvarchar` 문자열 코드로 설계합니다.

---

## 📋 2. 세부 개발 작업 체크리스트

### 🔹 Part 1: DB 엔티티 및 Enum/상수 모델링 (`mes_server/Models/`)
- [ ] `Models/MasterData/EquipmentStatus.cs` (문자열 상수 클래스: `RUNNING`, `STOPPED`, `MAINTENANCE`, `ERROR`, `SETUP` 등)
- [ ] `Models/MasterData/Equipment.cs` (설비 기본 정보, 누적 가동/비가동 초, 현재 LOT ID)
- [ ] `Models/MasterData/DowntimeReasonMaster.cs` (비가동 사유 마스터: `자재 부족`, `설비 고장`, `툴 교체` 등)
- [ ] `Models/Production/DowntimeLog.cs` (비가동 시작/종료 시각, 지속시간, 사유코드, 메모)

### 🔹 Part 2: DbContext 등록 & 마이그레이션 (`mes_server/Data/`)
- [ ] `AppDbContext.cs`에 `DbSet<Equipment>`, `DbSet<DowntimeReasonMaster>`, `DbSet<DowntimeLog>` 등록
- [ ] `OnModelCreating`에 기본 설비 3대 및 비가동 사유 5개 초기 시딩 데이터(Seed Data) 구성
- [ ] `dotnet ef migrations add AddEquipmentMonitoring` 및 `dotnet ef database update` 실행

### 🔹 Part 3: DTO 및 비즈니스 서비스 레이어 (`mes_server/Services/` & `Models/DTOs/`)
- [ ] `Models/DTOs/EquipmentDto.cs` (설비 현황 DTO, 상태 변경 요청 DTO, 비가동 사유 등록 DTO)
- [ ] `Services/IEquipmentService.cs` 및 `EquipmentService.cs` 구현:
  - `GetEquipmentsAsync()`: 전체 설비 현황 조회
  - `ChangeStatusAsync(...)`: `RUNNING` ➔ `STOPPED` 시 `DowntimeLog` 생성, `STOPPED` ➔ `RUNNING` 시 마감 계산
  - `RegisterDowntimeReasonAsync(...)`: 비가동 사유 코드 및 메모 업데이트

### 🔹 Part 4: SignalR 실시간 통신 허브 (`mes_server/Hubs/`)
- [ ] `Hubs/EquipmentHub.cs` 또는 기존 Hub에 실시간 이벤트 정의:
  - `EquipmentStatusUpdated` (설비 상태 변경 이벤트)
  - `SensorCountUpdated` (양품/불량 자동 카운터 펄스 이벤트)
  - `DowntimeOccurred` (비가동 발생 알림)

### 🔹 Part 5: 가상 센서/PLC 백그라운드 시뮬레이터 (`mes_server/Services/`)
- [ ] `appsettings.json` 설정 추가: `"SensorSimulation": { "Enabled": true, "IntervalSeconds": 3 }`
- [ ] `Services/AutomatedSensorBackgroundService.cs` (`IHostedService`) 작성
  - 3초 마다 가동 중인 설비의 `CurrentLot` 양품 수량을 +1 자동 증가하고 SignalR `SensorCountUpdated` 송출

### 🔹 Part 6: REST API 컨트롤러 (`mes_server/Controllers/`)
- [ ] `Controllers/EquipmentController.cs` 작성
  - `GET /api/equipment` (전체 설비 현황 목록)
  - `GET /api/equipment/{id}/downtime-history` (설비별 비가동 이력)
  - `POST /api/equipment/{id}/status` (상태 변경 API)
  - `POST /api/equipment/downtime/{id}/reason` (비가동 사유 등록 API)
  - `POST /api/equipment/simulation/pulse` (수동 가상 펄스 발생 API)

---

## ⚙️ 3. 검증 및 완료 기준 (Acceptance Criteria)
1. `dotnet build` 수행 시 컴파일 경고/에러 없이 빌드 성공.
2. `dotnet ef database update` 실행 시 SQLite/SQLServer DB에 `Equipments`, `DowntimeReasonMasters`, `DowntimeLogs` 테이블 정상 생성.
3. Swagger UI (`/swagger`)에서 `GET /api/equipment` 호출 시 시딩 데이터 정상 응답 확인.
4. `POST /api/equipment/{id}/status`로 상태를 `STOPPED`로 변경 시 `DowntimeLogs` 테이블에 비가동 시작 레코드 생성 확인.

## 🔥 PR 제목  
`Feat: 백엔드 설비 가동/비가동 데이터 모델링, SignalR 연동 및 가상 센서 백그라운드 엔진 구축 (#1)`

---

## 📌 작업 내용  
설비 가동/비가동 실시간 관제 및 현장 자동화를 위한 백엔드 데이터 모델링, 비가동 이력 추적 서비스, SignalR 푸시 및 가상 센서 연동 엔진을 개발하였습니다.

1. **DB 엔티티 & 마이그레이션 (`mes_server/Models/`, `Data/`)**:
   - `Equipment.cs`: 설비 기본 정보 및 누적 가동/비가동 시간(`TotalRunningSeconds`, `TotalDowntimeSeconds`), 현재 투입 LOT 관리
   - `EquipmentStatus.cs`: `RUNNING`, `STOPPED`, `MAINTENANCE`, `ERROR` 등 상태를 확장 가능한 문자열 상수로 작성 (DB 호환성 확보)
   - `DowntimeReasonMaster.cs`: 비가동 사유 기준 정보 마스터 (자재부족, 설비고장, 툴교체 등)
   - `DowntimeLog.cs`: 비가동 발생 시작/종료 시각, 지속 초, 사유 코드 및 작업자 ID 매핑
   - `MESDbContext.cs`: 관계 제약조건 설정 (`DeleteBehavior.Restrict`) 및 EF Core Migration (`AddEquipmentMonitoring`) 반영 완료

2. **DTO & 서비스 레이어 (`mes_server/Services/`, `Models/DTOs/`)**:
   - `IEquipmentService` & `EquipmentService`:
     - 설비 상태 전이 Engine: `RUNNING` ➔ `STOPPED` 시 `DowntimeLog` 개시, `STOPPED` ➔ `RUNNING` 복귀 시 비가동 시간 초 단위 자동 계산 및 마감
     - 작업자 비가동 사유 등록 기능 (`RegisterDowntimeReasonAsync`)
     - SignalR `IHubContext<MesHub>` 주입으로 상태 변경 시 웹 클라이언트에 실시간 브로드캐스트발송

3. **실시간 센서 백그라운드 서비스 (`mes_server/Services/AutomatedSensorBackgroundService.cs`)**:
   - 현장 생산 지시 프로세스 연동: 현재 `RUNNING` 상태이고 `CurrentLotID`가 존재하는 설비에 한해 3초 마다 자동으로 양품 수량 +1 카운트 펄스를 SignalR로 송출

4. **REST API 컨트롤러 (`mes_server/Controllers/EquipmentController.cs`)**:
   - `GET /api/equipment`: 전체 설비 가동 현황 조회
   - `POST /api/equipment/status`: 설비 상태 변경 API (WPF/웹 공용)
   - `GET /api/equipment/downtime-reasons`: 비가동 사유 마스터 목록 조회
   - `POST /api/equipment/downtime/reason`: 작업자 비가동 사유 입력 등록 API
   - `GET /api/equipment/{id}/downtime-history`: 설비별 비가동 이력 조회

---

## ✅ 체크리스트  
- [x] `dotnet build` 수행 시 컴파일 에러 0개 확인 완료
- [x] `dotnet ef database update` 실행을 통해 SQL Server DB에 테이블 생성 완료 (`Equipments`, `DowntimeReasonMasters`, `DowntimeLogs`)
- [x] `User.UserID` 및 `Equipment.EquipmentID` 외래키 타입/길이 100% 일치검증 완료
- [x] 백그라운드 엔진이 `RUNNING` 상태의 설비에만 선택적으로 센서 펄스를 전송하도록 검증 완료

---

## 🚀 테스트 방법  
1. `mes_server` 프로젝트 실행:
   ```bash
   dotnet run
   ```
2. Swagger UI (`/swagger`)에서 `POST /api/equipment/status` 테스트:
   - Request Body:
     ```json
     {
       "equipmentID": "EQ-01",
       "newStatus": "RUNNING",
       "currentLotID": "LOT-20260805-001"
     }
     ```
3. 서버 콘솔창에서 3초마다 `⚡ [센서 카운트 +1]` 로그가 발송되는지 확인.
4. 다시 `newStatus: "STOPPED"` 로 변경 시 펄스 송출이 멈추고 `DowntimeLogs` 테이블에 정지 시작 시각이 저장되는지 확인.

---

## 💡 추가 논의할 사항  
- 추후 실제 현장에 WPF 및 PLC 통신 모듈(Modbus/OPC-UA)이 붙을 경우, 백엔드의 `AutomatedSensorBackgroundService`는 `appsettings.json` 설정(`SensorSimulation:Enabled: false`)을 통해 끄고 WPF가 `POST /api/equipment/status` API 또는 SignalR을 호출하도록 스위칭하면 100% 연동됩니다.

---

## 🙏 리뷰어에게 한마디  
백엔드 구조 및 DB 설계가 기존 프로젝트 표준 패턴(`ID` 대문자 표기, `MESDbContext` 조인 규칙 등)과 100% 일치하도록 검증되었습니다. 확인 후 승인 부탁드립니다!

using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;

namespace mes_server.Data
{
    public static class DbInitializer
    {
        public static void Initialize(MESDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. 공정 마스터 (ProcessMaster) - 8개 공정
            if (!context.ProcessMasters.Any())
            {
                var processes = new List<ProcessMaster>
                {
                    new ProcessMaster { ProcessID = 1, ProcessName = "원자재 입고 및 자재 관리", SequenceOrder = 1 },
                    new ProcessMaster { ProcessID = 2, ProcessName = "CNC 선삭(Turning)", SequenceOrder = 2 },
                    new ProcessMaster { ProcessID = 3, ProcessName = "CNC 밀링(Milling)", SequenceOrder = 3 },
                    new ProcessMaster { ProcessID = 4, ProcessName = "열처리(Heat Treatment)", SequenceOrder = 4 },
                    new ProcessMaster { ProcessID = 5, ProcessName = "연삭(Grinding)", SequenceOrder = 5 },
                    new ProcessMaster { ProcessID = 6, ProcessName = "세척(Cleaning)", SequenceOrder = 6 },
                    new ProcessMaster { ProcessID = 7, ProcessName = "최종 검사(Final Inspection)", SequenceOrder = 7 },
                    new ProcessMaster { ProcessID = 8, ProcessName = "포장 및 출하(Packing & Shipping)", SequenceOrder = 8 }
                };
                context.ProcessMasters.AddRange(processes);
                context.SaveChanges();
            }

            // 2. 설비 마스터 (Equipment) - 태성테크놀로지 5대 핵심 설비
            if (!context.Equipments.Any())
            {
                var equipments = new List<Equipment>
                {
                    new Equipment { EquipmentID = "CNC01", Name = "CNC 선반 #1 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC01-01", TotalRunningSeconds = 1240320, TotalDowntimeSeconds = 84480, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC02", Name = "CNC 선반 #2 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC02-01", TotalRunningSeconds = 1256700, TotalDowntimeSeconds = 68100, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC03", Name = "CNC 밀링 #1 (HYUNDAI WIA F500 - 노후)", Status = EquipmentStatus.Stopped, CurrentLotId = "LOT-CNC03-01", TotalRunningSeconds = 1014960, TotalDowntimeSeconds = 309840, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC04", Name = "CNC 밀링 #2 (HYUNDAI WIA F500D)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC04-01", TotalRunningSeconds = 1257060, TotalDowntimeSeconds = 67740, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC05", Name = "연삭기 (STUDER S33)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC05-01", TotalRunningSeconds = 1209060, TotalDowntimeSeconds = 115740, LastStatusChangedAt = DateTime.UtcNow }
                };
                context.Equipments.AddRange(equipments);
                context.SaveChanges();
            }

            // 3. 품목 마스터 (ProductMaster)
            if (!context.ProductMasters.Any())
            {
                var products = new List<ProductMaster>
                {
                    new ProductMaster { ProductID = "FG-SFT-100", ProductName = "변속기 메인 샤프트 ASSY (Ø35×L280mm)", ItemType = ItemType.FinishedProduct, StockQty = 1250, SafetyStock = 200 },
                    new ProductMaster { ProductID = "SF-SFT-010", ProductName = "1차 가공 샤프트 (Ø38×L285mm)", ItemType = ItemType.SemiFinishedProduct, StockQty = 450, SafetyStock = 100 },
                    new ProductMaster { ProductID = "SF-SFT-020", ProductName = "2차 정밀 샤프트 (Ø35.5×L282mm)", ItemType = ItemType.SemiFinishedProduct, StockQty = 380, SafetyStock = 100 },
                    new ProductMaster { ProductID = "SF-SFT-030", ProductName = "열처리 샤프트 (Ø35.5×L282mm)", ItemType = ItemType.SemiFinishedProduct, StockQty = 300, SafetyStock = 50 },
                    new ProductMaster { ProductID = "SF-SFT-040", ProductName = "연삭 샤프트 (Ø35×L280mm)", ItemType = ItemType.SemiFinishedProduct, StockQty = 520, SafetyStock = 100 },
                    new ProductMaster { ProductID = "RM-SCM-001", ProductName = "SCM440 환봉 (Ø40×L300mm)", ItemType = ItemType.RawMaterial, StockQty = 3500, SafetyStock = 500 }
                };
                context.ProductMasters.AddRange(products);
                context.SaveChanges();
            }

            // 4. 사용자 (User) - BCrypt 해시 비밀번호 적용
            if (!context.Users.Any())
            {
                string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
                var users = new List<User>
                {
                    new User { UserID = "admin", UserName = "관리자 (생산관리팀)", UserRole = "Admin", PasswordHash = defaultPasswordHash },
                    new User { UserID = "operator1", UserName = "이종원 (생산1팀)", UserRole = "Operator", PasswordHash = defaultPasswordHash },
                    new User { UserID = "operator2", UserName = "서봉준 (생산1팀)", UserRole = "Operator", PasswordHash = defaultPasswordHash },
                    new User { UserID = "operator3", UserName = "송은섭 (생산2팀)", UserRole = "Operator", PasswordHash = defaultPasswordHash }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // 5. 비가동 사유 마스터 (DowntimeReasonMaster)
            if (!context.DowntimeReasonMasters.Any())
            {
                var reasons = new List<DowntimeReasonMaster>
                {
                    new DowntimeReasonMaster { ReasonCode = "DT-BREAK-SPINDLE", ReasonName = "설비고장 - 스핀들 이상 (주요병목)", Category = "BREAKDOWN", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-BREAK-ELECT", ReasonName = "설비고장 - 전기 계통 이상", Category = "BREAKDOWN", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-SETUP-JIG", ReasonName = "품번교체 - 지그 교체", Category = "SETUP", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-SETUP-PROG", ReasonName = "품번교체 - 프로그램 변경", Category = "SETUP", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-SETUP-TOOL", ReasonName = "품번교체 - 공구 세팅", Category = "SETUP", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-WAIT-MAT", ReasonName = "자재대기 - 자재 불출 지연", Category = "WAITING", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-WAIT-PREV", ReasonName = "자재대기 - 전공정 대기", Category = "WAITING", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-TOOL-CHANGE", ReasonName = "공구교체 - 공구 마모 교체", Category = "TOOL", IsActive = true },
                    new DowntimeReasonMaster { ReasonCode = "DT-QUAL-ADJ", ReasonName = "품질문제 - 치수 이탈 조치", Category = "QUALITY", IsActive = true }
                };
                context.DowntimeReasonMasters.AddRange(reasons);
                context.SaveChanges();
            }

            // 6. 비가동 내역 (DowntimeLog) - 총 221건 시드 데이터 (DateTimeKind.Utc 적용 및 중첩 방지)
            if (!context.DowntimeLogs.Any())
            {
                var logs = new List<DowntimeLog>();
                DateTime baseTime = new DateTime(2025, 10, 1, 8, 0, 0, DateTimeKind.Utc);

                // CNC03 (최저 OEE 핵심 병목 설비) - 90건 (5시간 간격, 최대 120분 지속으로 중첩 완전 제거)
                string[] cnc03Reasons = { "DT-BREAK-SPINDLE", "DT-SETUP-JIG", "DT-WAIT-MAT", "DT-SETUP-PROG", "DT-SETUP-TOOL", "DT-BREAK-ELECT", "DT-WAIT-PREV" };
                string[] cnc03Memos = {
                    "스핀들 발열 및 과진동 발생 긴급 정지 점검",
                    "FG-SFT-100 전용 지그 교체 및 영점 측정",
                    "자재 불출 지연으로 인한 설비 가동 대기",
                    "CNC 밀링 가공 프로그램 셋업 및 수정",
                    "키홈 밀링 엔드밀 세팅 및 위치 측정",
                    "전기계통 이송센서 누전 긴급 점검",
                    "1공정 선삭 반제품 공급 대기"
                };

                for (int i = 0; i < 90; i++)
                {
                    int rIdx = i % cnc03Reasons.Length;
                    int durationMin = (rIdx == 0) ? (35 + (i * 7) % 85) : (15 + (i * 5) % 40); // 15분 ~ 120분
                    DateTime start = baseTime.AddHours(i * 5).AddMinutes((i * 11) % 30);

                    logs.Add(new DowntimeLog
                    {
                        EquipmentID = "CNC03",
                        StartedAt = start,
                        EndedAt = start.AddMinutes(durationMin),
                        DurationSeconds = durationMin * 60,
                        ReasonCode = cnc03Reasons[rIdx],
                        OperatorMemo = cnc03Memos[rIdx],
                        UserID = (i % 2 == 0) ? "operator1" : "operator2"
                    });
                }

                // CNC01 - 35건 (7시간 간격)
                for (int i = 0; i < 35; i++)
                {
                    DateTime start = baseTime.AddHours(i * 7).AddMinutes(10);
                    int durationMin = 10 + (i * 3) % 25;
                    logs.Add(new DowntimeLog
                    {
                        EquipmentID = "CNC01",
                        StartedAt = start,
                        EndedAt = start.AddMinutes(durationMin),
                        DurationSeconds = durationMin * 60,
                        ReasonCode = (i % 2 == 0) ? "DT-WAIT-MAT" : "DT-TOOL-CHANGE",
                        OperatorMemo = (i % 2 == 0) ? "원자재 환봉 입고 검사 대기" : "황삭 인서트 팁 교체",
                        UserID = "operator2"
                    });
                }

                // CNC02 - 30건 (8시간 간격)
                for (int i = 0; i < 30; i++)
                {
                    DateTime start = baseTime.AddHours(i * 8).AddMinutes(15);
                    int durationMin = 10 + (i * 4) % 20;
                    logs.Add(new DowntimeLog
                    {
                        EquipmentID = "CNC02",
                        StartedAt = start,
                        EndedAt = start.AddMinutes(durationMin),
                        DurationSeconds = durationMin * 60,
                        ReasonCode = (i % 3 == 0) ? "DT-QUAL-ADJ" : "DT-TOOL-CHANGE",
                        OperatorMemo = (i % 3 == 0) ? "치수 이탈 가공 보정" : "바이트 인서트 팁 교체",
                        UserID = "operator2"
                    });
                }

                // CNC04 - 32건 (7.5시간 간격)
                for (int i = 0; i < 32; i++)
                {
                    DateTime start = baseTime.AddHours(i * 7.5).AddMinutes(20);
                    int durationMin = 12 + (i * 3) % 30;
                    logs.Add(new DowntimeLog
                    {
                        EquipmentID = "CNC04",
                        StartedAt = start,
                        EndedAt = start.AddMinutes(durationMin),
                        DurationSeconds = durationMin * 60,
                        ReasonCode = (i % 2 == 0) ? "DT-SETUP-TOOL" : "DT-TOOL-CHANGE",
                        OperatorMemo = "공구 세팅 및 마모 교체",
                        UserID = "operator1"
                    });
                }

                // CNC05 - 34건 (7.2시간 간격)
                for (int i = 0; i < 34; i++)
                {
                    DateTime start = baseTime.AddHours(i * 7.2).AddMinutes(25);
                    int durationMin = 15 + (i * 2) % 35;
                    logs.Add(new DowntimeLog
                    {
                        EquipmentID = "CNC05",
                        StartedAt = start,
                        EndedAt = start.AddMinutes(durationMin),
                        DurationSeconds = durationMin * 60,
                        ReasonCode = "DT-TOOL-CHANGE",
                        OperatorMemo = "연삭석 드레싱 및 세팅",
                        UserID = "operator3"
                    });
                }

                context.DowntimeLogs.AddRange(logs);
                context.SaveChanges();

                // 설비별 TotalDowntimeSeconds를 생성된 시드 DowntimeLog 합계로 동적 업데이트
                var eqList = context.Equipments.ToList();
                foreach (var eq in eqList)
                {
                    var sumDowntime = context.DowntimeLogs
                        .Where(d => d.EquipmentID == eq.EquipmentID)
                        .Sum(d => d.DurationSeconds ?? 0);
                    eq.TotalDowntimeSeconds = sumDowntime;
                }
                context.SaveChanges();
            }

            // 7. 작업지시, LOT 및 생산 실적 (단일 트랜잭션 개념의 원자적 시딩 - 총 116건)
            if (!context.WorkOrders.Any() || !context.Lots.Any() || !context.Performances.Any())
            {
                if (context.Performances.Any()) context.Performances.RemoveRange(context.Performances);
                if (context.Lots.Any()) context.Lots.RemoveRange(context.Lots);
                if (context.WorkOrders.Any()) context.WorkOrders.RemoveRange(context.WorkOrders);
                context.SaveChanges();

                var workOrder = new WorkOrder
                {
                    ProductID = "FG-SFT-100",
                    TargetQty = 2500,
                    TotalGoodQty = 2390,
                    TotalBadQty = 45,
                    Status = OrderStatus.InProgress,
                    OrderDate = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    StartDate = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2025, 10, 31, 0, 0, 0, DateTimeKind.Utc)
                };
                context.WorkOrders.Add(workOrder);
                context.SaveChanges();

                var lots = new List<Lot>
                {
                    new Lot { LotID = "LOT-CNC01-01", OrderID = workOrder.OrderID, CurrentProcessID = 2, Status = LotStatus.WIP },
                    new Lot { LotID = "LOT-CNC02-01", OrderID = workOrder.OrderID, CurrentProcessID = 2, Status = LotStatus.WIP },
                    new Lot { LotID = "LOT-CNC03-01", OrderID = workOrder.OrderID, CurrentProcessID = 3, Status = LotStatus.WIP },
                    new Lot { LotID = "LOT-CNC04-01", OrderID = workOrder.OrderID, CurrentProcessID = 3, Status = LotStatus.WIP },
                    new Lot { LotID = "LOT-CNC05-01", OrderID = workOrder.OrderID, CurrentProcessID = 5, Status = LotStatus.WIP }
                };
                context.Lots.AddRange(lots);
                context.SaveChanges();

                var perfList = new List<Performance>();
                DateTime baseDate = new DateTime(2025, 10, 1, 9, 0, 0, DateTimeKind.Utc);

                // LOT-CNC01-01 (25건)
                for (int i = 0; i < 25; i++)
                {
                    perfList.Add(new Performance
                    {
                        WorkOrderID = workOrder.OrderID,
                        LotID = "LOT-CNC01-01",
                        ProcessID = 2,
                        UserID = "operator1",
                        InputQty = 60,
                        GoodQty = 59,
                        BadQty = 1,
                        WorkDate = baseDate.AddHours(i * 4)
                    });
                }

                // LOT-CNC02-01 (25건)
                for (int i = 0; i < 25; i++)
                {
                    perfList.Add(new Performance
                    {
                        WorkOrderID = workOrder.OrderID,
                        LotID = "LOT-CNC02-01",
                        ProcessID = 2,
                        UserID = "operator1",
                        InputQty = 62,
                        GoodQty = 61,
                        BadQty = 1,
                        WorkDate = baseDate.AddHours(i * 4.1)
                    });
                }

                // LOT-CNC03-01 (20건)
                for (int i = 0; i < 20; i++)
                {
                    perfList.Add(new Performance
                    {
                        WorkOrderID = workOrder.OrderID,
                        LotID = "LOT-CNC03-01",
                        ProcessID = 3,
                        UserID = "operator2",
                        InputQty = 40,
                        GoodQty = 37,
                        BadQty = 3,
                        WorkDate = baseDate.AddHours(i * 5)
                    });
                }

                // LOT-CNC04-01 (23건)
                for (int i = 0; i < 23; i++)
                {
                    perfList.Add(new Performance
                    {
                        WorkOrderID = workOrder.OrderID,
                        LotID = "LOT-CNC04-01",
                        ProcessID = 3,
                        UserID = "operator2",
                        InputQty = 50,
                        GoodQty = 49,
                        BadQty = 1,
                        WorkDate = baseDate.AddHours(i * 4.5)
                    });
                }

                // LOT-CNC05-01 (23건)
                for (int i = 0; i < 23; i++)
                {
                    perfList.Add(new Performance
                    {
                        WorkOrderID = workOrder.OrderID,
                        LotID = "LOT-CNC05-01",
                        ProcessID = 5,
                        UserID = "operator3",
                        InputQty = 55,
                        GoodQty = 54,
                        BadQty = 1,
                        WorkDate = baseDate.AddHours(i * 4.4)
                    });
                }

                context.Performances.AddRange(perfList);
                context.SaveChanges();
            }
        }
    }
}

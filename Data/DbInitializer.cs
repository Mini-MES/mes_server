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
                    new Equipment { EquipmentID = "CNC01", Name = "CNC 선반 #1 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC01-01", TotalRunningSeconds = 0, TotalDowntimeSeconds = 0, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC02", Name = "CNC 선반 #2 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC02-01", TotalRunningSeconds = 0, TotalDowntimeSeconds = 0, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC03", Name = "CNC 밀링 #1 (HYUNDAI WIA F500)", Status = EquipmentStatus.Stopped, CurrentLotId = "LOT-CNC03-01", TotalRunningSeconds = 0, TotalDowntimeSeconds = 0, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC04", Name = "CNC 밀링 #2 (HYUNDAI WIA F500D)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC04-01", TotalRunningSeconds = 0, TotalDowntimeSeconds = 0, LastStatusChangedAt = DateTime.UtcNow },
                    new Equipment { EquipmentID = "CNC05", Name = "연삭기 (STUDER S33)", Status = EquipmentStatus.Running, CurrentLotId = "LOT-CNC05-01", TotalRunningSeconds = 0, TotalDowntimeSeconds = 0, LastStatusChangedAt = DateTime.UtcNow }
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
                        // ===== 설비고장 =====
                        new() { ReasonCode = "EQ001", ReasonName = "베어링 마모", Category = "설비고장", IsActive = true },
                        new() { ReasonCode = "EQ002", ReasonName = "블록 원인 분석", Category = "설비고장", IsActive = true },
                        new() { ReasonCode = "EQ003", ReasonName = "센서 불량", Category = "설비고장", IsActive = true },
                        new() { ReasonCode = "EQ004", ReasonName = "스핀들 이상", Category = "설비고장", IsActive = true },
                        new() { ReasonCode = "EQ005", ReasonName = "유압 누유", Category = "설비고장", IsActive = true },
                        new() { ReasonCode = "EQ006", ReasonName = "전기 계통 이상", Category = "설비고장", IsActive = true },

                        // ===== 공구/셋업 =====
                        new() { ReasonCode = "TL001", ReasonName = "공구 세팅", Category = "공구교체", IsActive = true },
                        new() { ReasonCode = "TL002", ReasonName = "드릴 교체", Category = "공구교체", IsActive = true },
                        new() { ReasonCode = "TL003", ReasonName = "엔드밀 교체", Category = "공구교체", IsActive = true },
                        new() { ReasonCode = "TL004", ReasonName = "연삭휠 교체", Category = "공구교체", IsActive = true },
                        new() { ReasonCode = "TL005", ReasonName = "인서트 교체", Category = "공구교체", IsActive = true },
                        new() { ReasonCode = "TL006", ReasonName = "지그 교체", Category = "공구교체", IsActive = true },

                        // ===== 자재 =====
                        new() { ReasonCode = "MT001", ReasonName = "원자재 미입고", Category = "자재", IsActive = true },
                        new() { ReasonCode = "MT002", ReasonName = "자재 불출 지연", Category = "자재", IsActive = true },

                        // ===== 생산대기 =====
                        new() { ReasonCode = "WT001", ReasonName = "전공정 대기", Category = "대기", IsActive = true },

                        // ===== 품질 =====
                        new() { ReasonCode = "QA001", ReasonName = "재가공", Category = "품질", IsActive = true },
                        new() { ReasonCode = "QA002", ReasonName = "초도품 검사", Category = "품질", IsActive = true },
                        new() { ReasonCode = "QA003", ReasonName = "치수 이탈 조치", Category = "품질", IsActive = true },

                        // ===== 계획 =====
                        new() { ReasonCode = "PL001", ReasonName = "프로그램 변경", Category = "계획", IsActive = true }
                    };
                context.DowntimeReasonMasters.AddRange(reasons);
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

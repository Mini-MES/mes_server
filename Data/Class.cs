using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using Microsoft.EntityFrameworkCore;

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
                        new Equipment { EquipmentID = "CNC01", Name = "CNC 선반 #1 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, TotalRunningSeconds = 1240320, TotalDowntimeSeconds = 84480,
  LastStatusChangedAt = DateTime.UtcNow },
                        new Equipment { EquipmentID = "CNC02", Name = "CNC 선반 #2 (DOOSAN PUMA 2600)", Status = EquipmentStatus.Running, TotalRunningSeconds = 1256700, TotalDowntimeSeconds = 68100,
  LastStatusChangedAt = DateTime.UtcNow },
                        new Equipment { EquipmentID = "CNC03", Name = "CNC 밀링 #1 (HYUNDAI WIA F500 - 노후)", Status = EquipmentStatus.Stopped, TotalRunningSeconds = 1014960, TotalDowntimeSeconds =
  309840, LastStatusChangedAt = DateTime.UtcNow },
                        new Equipment { EquipmentID = "CNC04", Name = "CNC 밀링 #2 (HYUNDAI WIA F500D)", Status = EquipmentStatus.Running, TotalRunningSeconds = 1257060, TotalDowntimeSeconds = 67740,
  LastStatusChangedAt = DateTime.UtcNow },
                        new Equipment { EquipmentID = "CNC05", Name = "연삭기 (STUDER S33)", Status = EquipmentStatus.Running, TotalRunningSeconds = 1209060, TotalDowntimeSeconds = 115740,
  LastStatusChangedAt = DateTime.UtcNow }
                    };
                context.Equipments.AddRange(equipments);
                context.SaveChanges();
            }

            // 3. 품목 마스터 (ProductMaster)                                                                                                                                                      
            if (!context.ProductMasters.Any())
            {
                var products = new List<ProductMaster>
                    {
                        new ProductMaster { ProductID = "FG-SFT-100", ProductName = "변속기 메인 샤프트 ASSY (Ø35×L280mm)", ItemType = ItemType.Product, StockQty = 1250, SafetyStock = 200 },
                        new ProductMaster { ProductID = "SF-SFT-010", ProductName = "1차 가공 샤프트 (Ø38×L285mm)", ItemType = ItemType.SemiProduct, StockQty = 450, SafetyStock = 100 },
                        new ProductMaster { ProductID = "SF-SFT-020", ProductName = "2차 정밀 샤프트 (Ø35.5×L282mm)", ItemType = ItemType.SemiProduct, StockQty = 380, SafetyStock = 100 },
                        new ProductMaster { ProductID = "SF-SFT-030", ProductName = "열처리 샤프트 (Ø35.5×L282mm)", ItemType = ItemType.SemiProduct, StockQty = 300, SafetyStock = 50 },
                        new ProductMaster { ProductID = "SF-SFT-040", ProductName = "연삭 샤프트 (Ø35×L280mm)", ItemType = ItemType.SemiProduct, StockQty = 520, SafetyStock = 100 },
                        new ProductMaster { ProductID = "RM-SCM-001", ProductName = "SCM440 환봉 (Ø40×L300mm)", ItemType = ItemType.RawMaterial, StockQty = 3500, SafetyStock = 500 }
                    };
                context.ProductMasters.AddRange(products);
                context.SaveChanges();
            }

            // 4. 사용자 (User)                                                                                                                                                                    
            if (!context.Users.Any())
            {
                var users = new List<User>
                    {
                        new User { UserID = "admin", UserName = "관리자 (생산관리팀)", Role = UserRole.Admin, UserPassword = "password123" },
                        new User { UserID = "operator1", UserName = "이종원 (생산1팀)", Role = UserRole.Operator, UserPassword = "password123" },
                        new User { UserID = "operator2", UserName = "서봉준 (생산1팀)", Role = UserRole.Operator, UserPassword = "password123" },
                        new User { UserID = "operator3", UserName = "송은섭 (생산2팀)", Role = UserRole.Operator, UserPassword = "password123" }
                    };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // 5. 비가동 사유 마스터 (DowntimeReasonMaster)                                                                                                                                        
            if (!context.DowntimeReasonMasters.Any())
            {
                var reasons = new List<DowntimeReasonMaster>
                    {
                        new DowntimeReasonMaster { ReasonCode = "DT-BREAK-SPINDLE", ReasonName = "설비고장 - 스핀들 이상 (주요병목)", Description = "스핀들 누유 및 이상 소음발생으로 인한 정지" },
                        new DowntimeReasonMaster { ReasonCode = "DT-BREAK-ELECT", ReasonName = "설비고장 - 전기 계통 이상", Description = "전력 과부하 및 센서 작동 오류" },
                        new DowntimeReasonMaster { ReasonCode = "DT-SETUP-JIG", ReasonName = "품번교체 - 지그 교체", Description = "품번 변경에 따른 셋업 툴 및 지그 교체" },
                        new DowntimeReasonMaster { ReasonCode = "DT-SETUP-PROG", ReasonName = "품번교체 - 프로그램 변경", Description = "NC 가공 프로그램 로딩 및 세팅" },
                        new DowntimeReasonMaster { ReasonCode = "DT-SETUP-TOOL", ReasonName = "품번교체 - 공구 세팅", Description = "엔드밀 및 바이트 치수 세팅" },
                        new DowntimeReasonMaster { ReasonCode = "DT-WAIT-MAT", ReasonName = "자재대기 - 자재 불출 지연", Description = "창고 불출 및 키팅 지연으로 인한 대기" },
                        new DowntimeReasonMaster { ReasonCode = "DT-WAIT-PREV", ReasonName = "자재대기 - 전공정 대기", Description = "전공정 가공 지연으로 인한 라인 대기" },
                        new DowntimeReasonMaster { ReasonCode = "DT-TOOL-CHANGE", ReasonName = "공구교체 - 공구 마모 교체", Description = "인서트/연삭석/엔드밀 마모 교체" },
                        new DowntimeReasonMaster { ReasonCode = "DT-QUAL-ADJ", ReasonName = "품질문제 - 치수 이탈 조치", Description = "초도품 검사 치수 오차 영점 재조정" }
                    };
                context.DowntimeReasonMasters.AddRange(reasons);
                context.SaveChanges();
            }

            // 6. 비가동 내역 (DowntimeLog) - CNC03 3대 핵심 병목원인 포함                                                                                                                         
            if (!context.DowntimeLogs.Any())
            {
                var logs = new List<DowntimeLog>
                    {                                                                                                                                                                                  
                        // CNC03 대형 스핀들 고장 2건 (811분 정지)                                                                                                                                     
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 7, 8, 58, 0), EndedAt = new DateTime(2025, 10, 7, 15, 10, 0), DurationSeconds = 372 * 60,
  ReasonCode = "DT-BREAK-SPINDLE", OperatorMemo = "스핀들 발열 및 과진동 발생. 긴급 수리 진행", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 8, 9, 24, 0), EndedAt = new DateTime(2025, 10, 8, 16, 43, 0), DurationSeconds = 439 * 60,
  ReasonCode = "DT-BREAK-SPINDLE", OperatorMemo = "스핀들 재작동 오류 및 부품 수급 교체 수리", UserID = "operator1" },                                                                                 
                                                                                                                                                                                                       
                        // CNC03 셋업 손실 반복 발생 기록                                                                                                                                              
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 1, 8, 38, 0), EndedAt = new DateTime(2025, 10, 1, 10, 3, 0), DurationSeconds = 85 * 60, ReasonCode
  = "DT-BREAK-ELECT", OperatorMemo = "전기계통 이송센서 누전 점검", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 2, 9, 20, 0), EndedAt = new DateTime(2025, 10, 2, 10, 10, 0), DurationSeconds = 50 * 60, ReasonCode
  = "DT-SETUP-JIG", OperatorMemo = "FG-SFT-100용 전용 지그 세팅", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 3, 8, 28, 0), EndedAt = new DateTime(2025, 10, 3, 9, 19, 0), DurationSeconds = 51 * 60, ReasonCode
  = "DT-SETUP-PROG", OperatorMemo = "프로그램 수정 및 교체", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 3, 10, 34, 0), EndedAt = new DateTime(2025, 10, 3, 11, 29, 0), DurationSeconds = 55 * 60,
  ReasonCode = "DT-SETUP-TOOL", OperatorMemo = "키홈 밀링 엔드밀 세팅", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 6, 9, 3, 0), EndedAt = new DateTime(2025, 10, 6, 9, 47, 0), DurationSeconds = 44 * 60, ReasonCode =
  "DT-SETUP-JIG", OperatorMemo = "지그 교체 및 영점조정", UserID = "operator1" },                                                                                                                      
                                                                                                                                                                                                       
                        // 자재대기 로그                                                                                                                                                               
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 2, 10, 36, 0), EndedAt = new DateTime(2025, 10, 2, 11, 23, 0), DurationSeconds = 47 * 60,
  ReasonCode = "DT-WAIT-MAT", OperatorMemo = "자재 불출 지연으로 인한 가동 대기", UserID = "operator2" },
                        new DowntimeLog { EquipmentID = "CNC03", StartedAt = new DateTime(2025, 10, 6, 10, 4, 0), EndedAt = new DateTime(2025, 10, 6, 10, 31, 0), DurationSeconds = 27 * 60, ReasonCode
  = "DT-WAIT-PREV", OperatorMemo = "1공정(선삭) 1차 가공품 공급 대기", UserID = "operator2" },                                                                                                         
                                                                                                                                                                                                       
                        // 타 설비 일상 비가동 로그                                                                                                                                                    
                        new DowntimeLog { EquipmentID = "CNC01", StartedAt = new DateTime(2025, 10, 1, 8, 23, 0), EndedAt = new DateTime(2025, 10, 1, 8, 34, 0), DurationSeconds = 11 * 60, ReasonCode
  = "DT-WAIT-MAT", OperatorMemo = "원자재 환봉 입고 검사 대기", UserID = "operator2" },
                        new DowntimeLog { EquipmentID = "CNC02", StartedAt = new DateTime(2025, 10, 1, 8, 32, 0), EndedAt = new DateTime(2025, 10, 1, 8, 43, 0), DurationSeconds = 11 * 60, ReasonCode
  = "DT-TOOL-CHANGE", OperatorMemo = "황삭 인서트 팁 교체", UserID = "operator2" },
                        new DowntimeLog { EquipmentID = "CNC04", StartedAt = new DateTime(2025, 10, 1, 9, 1, 0), EndedAt = new DateTime(2025, 10, 1, 9, 14, 0), DurationSeconds = 13 * 60, ReasonCode =
  "DT-TOOL-CHANGE", OperatorMemo = "스플라인 카터 공구 마모 교체", UserID = "operator1" },
                        new DowntimeLog { EquipmentID = "CNC05", StartedAt = new DateTime(2025, 10, 1, 8, 43, 0), EndedAt = new DateTime(2025, 10, 1, 9, 11, 0), DurationSeconds = 28 * 60, ReasonCode
  = "DT-TOOL-CHANGE", OperatorMemo = "연삭석 드레싱 및 교체", UserID = "operator3" }
                    };
                context.DowntimeLogs.AddRange(logs);
                context.SaveChanges();
            }

            // 7. 작업지시 및 생산 실적                                                                                                                                                            
            if (!context.WorkOrders.Any())
            {
                var workOrder = new WorkOrder
                {
                    ProductID = "FG-SFT-100",
                    TargetQty = 2500,
                    TotalGoodQty = 2390,
                    TotalBadQty = 45,
                    Status = OrderStatus.Completed,
                    OrderDate = new DateTime(2025, 10, 1),
                    StartDate = new DateTime(2025, 10, 1),
                    DueDate = new DateTime(2025, 10, 31)
                };
                context.WorkOrders.Add(workOrder);
                context.SaveChanges();

                var perfList = new List<Performance>
                    {
                        new Performance { WorkOrderID = workOrder.OrderID, LotID = "LOT-202510-01", ProcessID = 2, UserID = "operator1", InputQty = 1573, GoodQty = 1550, BadQty = 23, WorkDate = new
  DateTime(2025, 10, 31) },
                        new Performance { WorkOrderID = workOrder.OrderID, LotID = "LOT-202510-02", ProcessID = 2, UserID = "operator1", InputQty = 1648, GoodQty = 1625, BadQty = 23, WorkDate = new
  DateTime(2025, 10, 31) },
                        new Performance { WorkOrderID = workOrder.OrderID, LotID = "LOT-202510-03", ProcessID = 3, UserID = "operator2", InputQty = 753, GoodQty = 726, BadQty = 27, WorkDate = new
  DateTime(2025, 10, 31) },
                        new Performance { WorkOrderID = workOrder.OrderID, LotID = "LOT-202510-04", ProcessID = 3, UserID = "operator2", InputQty = 1067, GoodQty = 1044, BadQty = 23, WorkDate = new
  DateTime(2025, 10, 31) },
                        new Performance { WorkOrderID = workOrder.OrderID, LotID = "LOT-202510-05", ProcessID = 5, UserID = "operator3", InputQty = 2207, GoodQty = 2161, BadQty = 46, WorkDate = new
  DateTime(2025, 10, 31) }
                    };
                context.Performances.AddRange(perfList);
                context.SaveChanges();
            }
        }
    }
}
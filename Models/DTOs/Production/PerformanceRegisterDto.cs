using mes_server.Models.Enum;

namespace mes_server.Models.DTOs.Production
{
    public class PerformanceRegisterDto
    {
        public string LotID { get; set; } = null!;
        public int WorkOrderID { get; set; }
        public int ProcessID { get; set; }
        public string? ToolID { get; set; }
        public ReasonCode? ReasonCode { get; set; }
        public int InputQty { get; set; }
        public int GoodQty { get; set; }
        public int BadQty { get; set; }
    }
}

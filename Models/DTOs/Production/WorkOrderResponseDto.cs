using mes_server.Models.Enum;
using System;

namespace mes_server.Models.DTOs.Production
{
    public class WorkOrderResponseDto
    {
        public int OrderID { get; set; }
        public string ProductID { get; set; } = null!;
        public int TargetQty { get; set; }
        public int TotalGoodQty { get; set; }
        public int TotalBadQty { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? LotID { get; set; }
    }
}

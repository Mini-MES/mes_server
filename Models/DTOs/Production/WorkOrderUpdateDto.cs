using mes_server.Models.Enum;

namespace mes_server.Models.DTOs.Production
{
    public class WorkOrderUpdateDto
    {
        public int TargetQty { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public OrderStatus Status { get; set; }
    }
}

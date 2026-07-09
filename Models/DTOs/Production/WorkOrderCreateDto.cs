namespace mes_server.Models.DTOs.Production
{
    public class WorkOrderCreateDto
    {
        public string ProductID { get; set; } = null!;
        public int TargetQty { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}

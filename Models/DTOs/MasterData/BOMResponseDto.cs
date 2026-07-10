namespace mes_server.Models.DTOs.MasterData
{
    public class BOMResponseDto
    {
        public string ProductID { get; set; } = null!;
        public string ChildProductID { get; set; } = null!;
        public string ChildProductName { get; set; } = null!;
        public int RequiredQty { get; set; }
        public int ProcessID { get; set; }
        public string? ProcessName { get; set; }
    }
}

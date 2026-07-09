namespace mes_server.Models.DTOs.MasterData
{
    public class BOMCreateDto
    {
        public string ProductID { get; set; } = null!;
        public string MaterialID { get; set; } = null!;
        public int ProcessID { get; set; }
        public int RequiredQty { get; set; }
    }
}

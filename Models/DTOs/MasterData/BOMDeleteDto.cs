namespace mes_server.Models.DTOs.MasterData
{
    public class BOMDeleteDto
    {
        public string ProductID { get; set; } = null!;
        public string MaterialID { get; set; } = null!;
        public int ProcessID { get; set; }
    }
}

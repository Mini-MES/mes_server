namespace mes_server.Models.DTOs.MasterData
{
    public class ProcessCreateDto
    {
        public string ProcessName { get; set; } = null!;
        public int SequenceOrder { get; set; }
    }
}

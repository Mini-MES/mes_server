namespace mes_server.Models.DTOs.Tool
{
    public class CreateToolDto
    {
        public string ToolID { get; set; } = null!;
        public string ToolName { get; set; } = null!;
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
    }
}

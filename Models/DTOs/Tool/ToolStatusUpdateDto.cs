using mes_server.Models.Enum;

namespace mes_server.Models.DTOs.Tool
{
    public class ToolStatusUpdateDto
    {
        public ToolStatus Status { get; set; }
        public ReasonCode Reason { get; set; }
    }
}

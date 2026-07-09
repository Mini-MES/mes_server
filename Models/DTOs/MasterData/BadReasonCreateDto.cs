using mes_server.Models.Enum;

namespace mes_server.Models.DTOs.MasterData
{
    public class BadReasonCreateDto
    {
        public ReasonCode ReasonCode { get; set; }
        public string Description { get; set; } = null!;
    }
}

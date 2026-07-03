using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.Production
{
    public class Tool
    {
        [Key]
        [Required]
        public string ToolID { get; set; } = null!;

        [Required]
        public string ToolName { get; set; } = null!;

        [Required]
        public int MaxUsageCount { get; set; }

        [Required]
        public int CurrentUsageCount { get; set; }
    }
}

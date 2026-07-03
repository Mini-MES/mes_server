using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string UserRole { get; set; } = "Operator";

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}

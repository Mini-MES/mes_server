using System.ComponentModel.DataAnnotations;

namespace mes_server.Models.MasterData
{
    public class User
    {
        [Key]
        public string UserID { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = null!;

        [Required]
        public string UserRole { get; set; } = "Operator";

        [Required]
        public string PasswordHash { get; set; } = null!;
    }
}

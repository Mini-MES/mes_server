namespace mes_server.Models.DTOs.MasterData
{
    public class UserRegisterDto
    {
        public string UserID { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string UserRole { get; set; } = "Operator"; // 기본값을 Operator로 설정
    }
}

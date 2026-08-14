using mes_server.Models.DTOs.MasterData;

namespace mes_server.Services.AuthService
{
    public interface IAuthService
    {
        Task<(string token, string refreshToken)> LoginAsync(LoginDto loginDto);
        Task<(string token, string refreshToken)> RefreshTokenAsync(string token);
    }
}

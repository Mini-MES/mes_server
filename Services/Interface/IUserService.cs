using mes_server.Models.DTOs.MasterData;

namespace mes_server.Services.Interface
{
    public interface IUserService
    {
        Task<(string token, string refreshToken)> LoginAsync(LoginDto loginDto);
        Task RegisterUserAsync(UserRegisterDto dto);
        Task<bool> AuthenticateAsync(string userName, string password);
        Task UpdateUserRoleAsync(string userId, string newRole);
        Task<(string token, string refreshToken)> RefreshTokenAsync(string token);
    }
}

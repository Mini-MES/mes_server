using mes_server.Models.DTOs.MasterData;

namespace mes_server.Services.UserService
{
    public interface IUserService
    {
        Task RegisterUserAsync(UserRegisterDto dto);
        Task<bool> UpdateUserRoleAsync(string userId, string newRole);
    }
}

using mes_server.Models.DTOs.MasterData;

namespace mes_server.Services.UserSvc
{
    public interface IUserService
    {
        Task RegisterUserAsync(UserRegisterDto dto);
        Task UpdateUserRoleAsync(string userId, string newRole);
    }
}

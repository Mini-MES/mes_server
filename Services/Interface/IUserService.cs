using mes_server.Models.MasterData;

namespace mes_server.Services.Interface
{
    public interface IUserService
    {
        Task RegisterUserAsync(User user, string password);
        Task<bool> AuthenticateAsync(string userName, string password);
        Task UpdateUserRoleAsync(string userId, string newRole);
    }
}

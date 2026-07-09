using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.MasterData
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUserNameAsync(string userName);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
    }
}

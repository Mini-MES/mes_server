using mes_server.Data;
using mes_server.Models.MasterData;
using mes_server.Repositories.Generic;
using mes_server.Repositories.Interface.MasterData;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.MasterData
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MESDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await Context.Users.FirstOrDefaultAsync(x => x.UserName == userName);
        }
    }
}

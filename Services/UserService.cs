using mes_server.Data;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.MasterData;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly MESDbContext _context;

        public UserService(IUserRepository userRepository, MESDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public async Task RegisterUserAsync(User user, string password)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            await _userRepository.CreateAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AuthenticateAsync(string userName, string password)
        {
            var user = await _userRepository.GetByUserNameAsync(userName);
            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task UpdateUserRoleAsync(string userId, string newRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.UserRole = newRole;
                await _userRepository.UpdateAsync(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}

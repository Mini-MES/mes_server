using mes_server.Data;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.MasterData;

namespace mes_server.Services.UserSvc
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        

        public async Task RegisterUserAsync(UserRegisterDto dto)
        {
            if (await _userRepository.GetByUserIDAsync(dto.UserID) != null)
            {
                throw new InvalidOperationException("UserID already exists.");
            }

            var user = new User
            {
                UserID = dto.UserID,
                UserName = dto.UserName,
                UserRole = dto.UserRole
            };
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _userRepository.CreateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task UpdateUserRoleAsync(string userId, string newRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.UserRole = newRole;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            }
        }
    }
}

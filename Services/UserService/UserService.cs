using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.MasterData;

namespace mes_server.Services.UserService
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
                UserRole = "Operator", // 기본 역할로 지정, 이후 변경하려면 UdpateRole 사용
            };
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _userRepository.CreateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.UserRole = newRole;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return true;
            }
            return false;
        }
    }
}

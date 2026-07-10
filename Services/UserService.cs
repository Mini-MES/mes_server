using mes_server.Data;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;
using mes_server.Models.Settings;
using mes_server.Repositories.Interface.MasterData;
using mes_server.Services.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace mes_server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly MESDbContext _context;

        public UserService(IUserRepository userRepository, IOptions<JwtSettings> jwtOptions, MESDbContext context)
        {
            _userRepository = userRepository;
            _jwtSettings = jwtOptions.Value;
            _context = context;
        }

        public string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
          

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.UserRole) 
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32]; 
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<(string token, string refreshToken)> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUserIDAsync(loginDto.UserID);
            if(user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); 
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return (token, refreshToken);
        }

        public async Task<(string token, string refreshToken)> RefreshTokenAsync(string token)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(token);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("리프레시 토큰 만료");

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return (newToken, newRefreshToken);
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

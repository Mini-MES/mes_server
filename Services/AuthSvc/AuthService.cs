using mes_server.Models.DTOs.MasterData;
using mes_server.Models.MasterData;
using mes_server.Models.Settings;
using mes_server.Repositories.MasterData;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace mes_server.Services.AuthSvc
{
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserRepository _userRepository;

        public AuthService(IOptions<JwtSettings> jwtOptions, UserRepository userRepository)
        {
            _jwtSettings = jwtOptions.Value;
            _userRepository = userRepository;
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

        public async Task<(string token, string refreshToken)> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUserIDAsync(loginDto.UserID);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

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
            await _userRepository.SaveChangesAsync();

            return (newToken, newRefreshToken);
        }

        public async Task<bool> AuthenticateAsync(string userName, string password)
        {
            var user = await _userRepository.GetByUserNameAsync(userName);
            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
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
    }
}

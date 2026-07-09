using mes_server.Models.DTOs.MasterData;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mes_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService) => _userService = userService;

        // 회원가입
        [HttpPost("signup")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            await _userService.RegisterUserAsync(dto);
            return Ok(new { Message = "사용자가 생성되었습니다." });
        }

        // 로그인
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var (token, refreshToken) = await _userService.LoginAsync(dto);
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions { HttpOnly = true, Secure = true });
            return Ok(new { Token = token });
        }

        // 리프레쉬 토큰
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken)) return BadRequest("Token missing");
            var newTokens = await _userService.RefreshTokenAsync(refreshToken);
            Response.Cookies.Append("refreshToken", newTokens.refreshToken, new CookieOptions { HttpOnly = true, Secure = true });
            return Ok(new { Token = newTokens.token });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("users/{id}/role")]
        [Authorize(Roles = "Admin")] // 관리자만 권한 수정 가능
        public async Task<IActionResult> UpdateRole([FromRoute] string id, [FromBody] string newRole)
        {
            await _userService.UpdateUserRoleAsync(id, newRole);
            return Ok(new { Message = "권한이 업데이트되었습니다." });
        }
    }
}

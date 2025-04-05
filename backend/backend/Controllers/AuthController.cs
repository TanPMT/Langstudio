using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.Register(request.Email, request.Password);
                return Ok(new { Message = "Registered. Please verify your email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _userService.Login(request.Email, request.Password);
            if (token == null) return Unauthorized();
            return Ok(new { Token = token });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyRequest request)
        {
            var result = await _userService.VerifyCode(request.Email, request.Code);
            return result ? Ok("Verified") : BadRequest("Invalid code");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.SendPasswordResetLink(request.Email);
            return Ok("Reset link sent");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _userService.ResetPassword(request.Email, request.Token, request.NewPassword);
            return result ? Ok("Password reset") : BadRequest("Invalid token or expired");
        }
    }

    public class RegisterRequest { public string Email { get; set; } public string Password { get; set; } }
    public class LoginRequest { public string Email { get; set; } public string Password { get; set; } }
    public class VerifyRequest { public string Email { get; set; } public string Code { get; set; } }
    public class ForgotPasswordRequest { public string Email { get; set; } }
    public class ResetPasswordRequest { public string Email { get; set; } public string Token { get; set; } public string NewPassword { get; set; } }
}
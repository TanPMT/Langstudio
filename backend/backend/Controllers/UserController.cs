using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bảo vệ tất cả endpoint
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env; // Để lưu file

        public UserController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = await _userService.GetUserInfo(email);
            if (user == null) return Unauthorized();
            return Ok(user);
        }

        [HttpPost("update-avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatar)
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            
            if (avatar == null || avatar.Length == 0)
                return BadRequest("No file uploaded");

            // Đường dẫn lưu file
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Tạo tên file duy nhất
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            // Cập nhật URL avatar trong database
            var avatarUrl = $"/uploads/avatars/{fileName}";
            await _userService.UpdateAvatar(email, avatarUrl);
            return Ok(new { Message = "Avatar updated", AvatarUrl = avatarUrl });
        }

        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (email != request.Email) return Unauthorized("You can only update your own password");
            await _userService.UpdatePassword(request.Email, request.NewPassword);
            return Ok("Password updated");
        }
    }

    public class UpdatePasswordRequest { public string Email { get; set; } public string NewPassword { get; set; } }
}
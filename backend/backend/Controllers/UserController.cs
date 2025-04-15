using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[Route("/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userService.GetCurrentUserInfoAsync(userId);
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName, 
            user.AvatarUrl
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.ChangePasswordAsync(userId, model);
        if (!result)
            return BadRequest("Password change failed");
        return Ok("Password changed successfully");
    }

    [HttpPost("update-avatar")]
    public async Task<IActionResult> UpdateAvatar([FromForm] UpdateAvatarModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var avatarUrl = await _userService.UpdateAvatarAsync(userId, model);
        return Ok(new { AvatarUrl = avatarUrl });
    }

    [HttpPost("update-fullname")]
    public async Task<IActionResult> UpdateFullName(UpdateFullNameModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var fullName = await _userService.UpdateFullNameAsync(userId, model);
        return Ok(fullName + " updated successfully");
    }
    
    [HttpPost("update-darkmode")]
    public async Task<IActionResult> UpdateDarkMode([FromBody] bool isDarkMode)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.UpdateDarkModeAsync(userId, isDarkMode);
        if (result == false)
            return Ok("Light mode updated successfully");
        else if (result == true)
            return Ok("Dark mode updated successfully");
        return Ok("Mode failed to update");
    }
}
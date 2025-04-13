using Microsoft.AspNetCore.Identity;
using backend.Data;
using backend.Models;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMinioService _minioService;

    public UserService(UserManager<ApplicationUser> userManager, IMinioService minioService)
    {
        _userManager = userManager;
        _minioService = minioService;
    }

    public async Task<ApplicationUser> GetCurrentUserInfoAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        return result.Succeeded;
    }

    public async Task<string> UpdateAvatarAsync(string userId, UpdateAvatarModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldFileName = Path.GetFileName(new Uri(user.AvatarUrl).AbsolutePath);
            await _minioService.DeleteFileAsync(oldFileName);
        }

        var avatarUrl = await _minioService.UploadFileAsync(model.Avatar);
        user.AvatarUrl = avatarUrl;
        await _userManager.UpdateAsync(user);

        return avatarUrl;
    }

    public async Task<string> UpdateFullNameAsync(string userId, UpdateFullNameModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");
        user.FullName = model.FullName;
        await _userManager.UpdateAsync(user);
        return user.FullName;   
    }
}
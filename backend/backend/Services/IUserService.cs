using backend.Models;

namespace backend.Services;

public interface IUserService
{
    Task<ApplicationUser> GetCurrentUserInfoAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel model);
    Task<string> UpdateAvatarAsync(string userId, UpdateAvatarModel model);
}
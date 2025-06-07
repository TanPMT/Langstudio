using Microsoft.AspNetCore.Identity;
using backend.Models;

namespace backend.Services;

public class ProService : IProService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> IsProUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsPro ?? false;
    }
}
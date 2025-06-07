using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsDarkMode { get; set; }
    public bool IsPro { get; set; }
}
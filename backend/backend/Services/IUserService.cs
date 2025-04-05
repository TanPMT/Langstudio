using backend.Models;

namespace backend.Services
{
    public interface IUserService
    {
        Task<User> Register(string email, string password);
        Task<string> Login(string email, string password);
        Task SendVerificationCode(string email);
        Task<bool> VerifyCode(string email, string code);
        Task SendPasswordResetLink(string email);
        Task<bool> ResetPassword(string email, string token, string newPassword);
        Task<User> GetUserInfo(string email);
        Task UpdateAvatar(string email, string avatarUrl);
        Task UpdatePassword(string email, string newPassword);
    }
}
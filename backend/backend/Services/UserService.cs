using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public UserService(ApplicationDbContext context, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _emailService = emailService;
            _config = config;
        }
        
        public async Task<User> Register(string email, string password)
        {
            // Kiểm tra email đã tồn tại
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                throw new Exception("Email already exists");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsVerified = false,
                VerificationCode = GenerateCode()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await SendVerificationCode(email);
            return user;
        }
        public async Task<string> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) || !user.IsVerified)
                return null;

            return GenerateJwtToken(user);
        }

        public async Task SendVerificationCode(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.VerificationCode = GenerateCode();
                await _context.SaveChangesAsync();
                await _emailService.SendEmailAsync(email, "Verification Code", $"Your code is: {user.VerificationCode}");
            }
        }

        public async Task<bool> VerifyCode(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && user.VerificationCode == code)
            {
                user.IsVerified = true;
                user.VerificationCode = null;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task SendPasswordResetLink(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.ResetToken = Guid.NewGuid().ToString();
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();
                var resetLink = $"http://localhost:5028/reset-password?email={email}&token={user.ResetToken}";
                await _emailService.SendEmailAsync(email, "Reset Password", $"Click here to reset: {resetLink}");
            }
        }

        public async Task<bool> ResetPassword(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
            if (user != null && user.ResetTokenExpiry > DateTime.UtcNow)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<User> GetUserInfo(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        
        public async Task UpdateAvatar(string email, string avatarUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) throw new Exception("User not found");
            user.AvatarUrl = avatarUrl; // Lưu URL của file đã upload
            await _context.SaveChangesAsync();
        }
        public async Task UpdatePassword(string email, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateCode() => new Random().Next(100000, 999999).ToString();
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
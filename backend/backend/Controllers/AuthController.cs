using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models;
using backend.Services;
using Microsoft.Extensions.Configuration;

namespace backend.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            // Nếu tài khoản chưa verify, xóa để cho phép đăng ký lại
            if (!existingUser.EmailConfirmed)
            {
                await _userManager.DeleteAsync(existingUser);
            }
            else
            {
                // Nếu đã verify, báo lỗi
                return BadRequest("Email is already taken.");
            }
        }
        var user = new ApplicationUser
        {
            UserName = model.Email, 
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = user.Id, token }, Request.Scheme);

        await _emailService.SendEmailAsync(model.Email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        return Ok("Registration successful. Please check your email to confirm.");
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return BadRequest("Invalid user");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return BadRequest("Email confirmation failed");

        return Ok("Email confirmed successfully");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized("Invalid credentials");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return BadRequest("Please confirm your email first");

        var token = GenerateJwtToken(user);
        // Set token vào cookie
        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(new { message = "Login successful" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {   
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPassWordModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.email);
        if (user == null)
            return BadRequest("User not found");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Action(nameof(ResetPassword), "Auth", new { model.email, token }, Request.Scheme);

        await _emailService.SendEmailAsync(model.email, "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        return Ok("Password reset link sent to your email");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return BadRequest("User not found");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Password reset successful");
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public string? VerificationCode { get; set; }
    public bool IsVerified { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}
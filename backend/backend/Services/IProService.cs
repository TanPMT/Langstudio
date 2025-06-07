namespace backend.Services;

public interface IProService
{
    Task<bool> IsProUserAsync(string userId);
}
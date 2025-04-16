using System.Threading.Tasks;

namespace backend.Services;

public interface IGeminiService
{
    Task<string> GenerateContentAsync(string prompt);
}
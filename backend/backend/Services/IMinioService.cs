namespace backend.Services;

public interface IMinioService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileName);
}
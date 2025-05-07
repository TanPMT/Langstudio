using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioService(IConfiguration configuration)
    {
        var endpoint = configuration["Minio:Endpoint"];
        var accessKey = configuration["Minio:AccessKey"];
        var secretKey = configuration["Minio:SecretKey"];
        _bucketName = configuration["Minio:BucketName"];

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();

        InitializeBucketAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeBucketAsync()
    {
        var beArgs = new BucketExistsArgs()
            .WithBucket(_bucketName);

        bool bucketExists = await _minioClient.BucketExistsAsync(beArgs);
        if (!bucketExists)
        {
            var mbArgs = new MakeBucketArgs()
                .WithBucket(_bucketName);
            await _minioClient.MakeBucketAsync(mbArgs);
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        using var stream = file.OpenReadStream();

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        var endpoint = _minioClient.Config.Endpoint.StartsWith("http://") || _minioClient.Config.Endpoint.StartsWith("https://")
            ? _minioClient.Config.Endpoint
            : $"http://42.96.13.119:9000";
        var url = $"http://api.scanvirus.me:9000/{_bucketName}/{fileName}";
        return url;
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName);

        await _minioClient.RemoveObjectAsync(removeObjectArgs);
    }
}

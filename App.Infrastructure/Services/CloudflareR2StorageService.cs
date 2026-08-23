using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using App.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Services;

public class CloudflareR2StorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<IAmazonS3> _s3Client;

    public CloudflareR2StorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _s3Client = new Lazy<IAmazonS3>(CreateS3Client);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var bucketName = _configuration["R2:BucketName"]!;
        var publicUrl = _configuration["R2:PublicUrl"]!;

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.Value.PutObjectAsync(request, cancellationToken);

        return $"{publicUrl}/{fileName}";
    }

    private IAmazonS3 CreateS3Client()
    {
        var serviceUrl = _configuration["R2:ServiceUrl"]!;
        var accessKeyId = _configuration["R2:AccessKeyId"]!;
        var secretAccessKey = _configuration["R2:SecretAccessKey"]!;

        var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        return new AmazonS3Client(credentials, config);
    }
}

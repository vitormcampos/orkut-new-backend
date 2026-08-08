using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using App.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Services;

public class CloudflareR2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public CloudflareR2StorageService(IConfiguration configuration)
    {
        var serviceUrl = configuration["R2:ServiceUrl"]!;
        var accessKeyId = configuration["R2:AccessKeyId"]!;
        var secretAccessKey = configuration["R2:SecretAccessKey"]!;
        _bucketName = configuration["R2:BucketName"]!;
        _publicUrl = configuration["R2:PublicUrl"]!;

        var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        return $"{_publicUrl}/{fileName}";
    }
}

namespace App.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
}

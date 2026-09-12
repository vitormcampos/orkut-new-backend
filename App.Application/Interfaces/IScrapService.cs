using App.Application.DTOs;

namespace App.Application.Interfaces;

public interface IScrapService
{
    Task<ScrapDto> CreateAsync(
        Guid authorId,
        CreateScrapRequest request,
        CancellationToken ct = default
    );

    Task<ScrapPageDto> GetProfileScrapsAsync(
        Guid profileId,
        Guid? viewerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default
    );

    Task DeleteAsync(Guid scrapId, Guid authorId, CancellationToken ct = default);
}

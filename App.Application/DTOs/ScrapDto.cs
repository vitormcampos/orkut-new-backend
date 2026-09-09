namespace App.Application.DTOs;

public record ScrapDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string AuthorUsername,
    string? AuthorProfilePicture,
    Guid RecipientId,
    string RecipientName,
    string RecipientUsername,
    string Content,
    string Visibility,
    DateTime CreatedAt
);

public record CreateScrapRequest(
    Guid RecipientId,
    string Content,
    string? Visibility = null
);

public record ScrapPageDto(
    IEnumerable<ScrapDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    bool HasNextPage
);

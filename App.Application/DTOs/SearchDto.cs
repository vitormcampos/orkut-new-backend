namespace App.Application.DTOs;

public record UserSearchResultDto(
    Guid Id,
    string Name,
    string Username,
    string? ProfilePicture
);

public record CommunitySearchResultDto(
    Guid Id,
    string Name,
    string? Description,
    string? Photo,
    int MemberCount
);

public record SearchPageDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    bool HasNextPage
);

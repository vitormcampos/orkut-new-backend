namespace App.Application.DTOs;

public record PostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string AuthorUsername,
    string? AuthorProfilePicture,
    Guid? CommunityId,
    string Content,
    DateTime CreatedAt);

public record CreatePostRequest(Guid? CommunityId, string Content);

public record PostPageDto(IEnumerable<PostDto> Items, int Page, int PageSize, int TotalCount);

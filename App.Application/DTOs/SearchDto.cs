namespace App.Application.DTOs;

public sealed record SearchQuery(string Term, int Limit = 10);

public sealed record UserSearchResultDto(Guid Id, string Name, string Username, string? ProfilePicture);

public sealed record CommunitySearchResultDto(Guid Id, string Name, string? Description, string? Photo, int MemberCount);

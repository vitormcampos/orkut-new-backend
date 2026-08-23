namespace App.Application.DTOs;

public record CommunityDto(Guid Id, string Name, string? Description, string? Photo, Guid OwnerId, string OwnerName, int MemberCount, DateTime CreatedAt, DateTime? UpdatedAt);

public record CommunityMemberDto(Guid CommunityId, Guid UserId, string UserName, string? UserProfilePicture, string Role, DateTime JoinedAt);

public record CreateCommunityRequest(string Name, string? Description);

public record UpdateCommunityRequest(string Name, string? Description);

using App.Application.DTOs;

namespace App.Application.Interfaces;

public interface ICommunityService
{
    Task<CommunityDto> CreateAsync(Guid ownerId, CreateCommunityRequest request, CancellationToken ct = default);
    Task<CommunityDto> UpdateAsync(Guid communityId, Guid currentUserId, UpdateCommunityRequest request, CancellationToken ct = default);
    Task<CommunityDto?> GetByIdAsync(Guid communityId, CancellationToken ct = default);
    Task<IReadOnlyList<CommunityDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CommunitySearchResultDto>> SearchAsync(string term, int limit = 10, CancellationToken ct = default);
    Task<SearchPageDto<CommunitySearchResultDto>> SearchPageAsync(string term, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task JoinAsync(Guid communityId, Guid userId, CancellationToken ct = default);
    Task LeaveAsync(Guid communityId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<CommunityMemberDto>> GetMembersAsync(Guid communityId, CancellationToken ct = default);
    Task<CommunityDto> SetPhotoAsync(Guid communityId, Guid currentUserId, string url, CancellationToken ct = default);
    Task<int> GetMemberCountAsync(Guid communityId, CancellationToken ct = default);
}

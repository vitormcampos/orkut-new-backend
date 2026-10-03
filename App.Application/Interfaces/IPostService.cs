using App.Application.DTOs;

namespace App.Application.Interfaces;

public interface IPostService
{
    Task<PostDto> CreateAsync(Guid authorId, CreatePostRequest request, CancellationToken ct = default);
    Task<PostPageDto> GetByUserAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PostPageDto> GetByCommunityAsync(Guid communityId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PostDto?> GetByIdAsync(Guid postId, CancellationToken ct = default);
    Task DeleteAsync(Guid postId, Guid currentUserId, CancellationToken ct = default);
}

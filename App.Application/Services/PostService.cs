using App.Application.DTOs;
using App.Application.Exceptions;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class PostService : IPostService
{
    private readonly DbContext _context;

    public PostService(DbContext context) => _context = context;

    public async Task<PostDto> CreateAsync(Guid authorId, CreatePostRequest request, CancellationToken ct = default)
    {
        if (request.CommunityId.HasValue)
        {
            var community = await _context.Set<Community>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.CommunityId.Value, ct);
            if (!community)
                throw new CommunityNotFoundException(request.CommunityId.Value);

            var isMember = await _context.Set<CommunityMember>()
                .AnyAsync(m => m.CommunityId == request.CommunityId.Value && m.UserId == authorId, ct);
            if (!isMember)
                throw new NotCommunityMemberException();
        }

        var post = new Post(authorId, request.CommunityId, request.Content);
        _context.Set<Post>().Add(post);
        await _context.SaveChangesAsync(ct);

        return await MapPostAsync(post.Id, ct) ?? throw new PostNotFoundException(post.Id);
    }

    public Task<PostPageDto> GetByUserAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken ct = default) =>
        GetPageAsync(p => p.AuthorId == userId && p.CommunityId == null, page, pageSize, ct);

    public Task<PostPageDto> GetByCommunityAsync(Guid communityId, int page = 1, int pageSize = 10, CancellationToken ct = default) =>
        GetPageAsync(p => p.CommunityId == communityId, page, pageSize, ct);

    public Task<PostDto?> GetByIdAsync(Guid postId, CancellationToken ct = default) => MapPostAsync(postId, ct);

    public async Task DeleteAsync(Guid postId, Guid currentUserId, CancellationToken ct = default)
    {
        var post = await _context.Set<Post>()
            .Include(p => p.Community)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null)
            throw new PostNotFoundException(postId);

        var isAuthor = post.AuthorId == currentUserId;
        var isCommunityOwner = post.CommunityId.HasValue && post.Community!.OwnerId == currentUserId;
        if (!isAuthor && !isCommunityOwner)
            throw new UnauthorizedPostActionException();

        _context.Set<Post>().Remove(post);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<PostPageDto> GetPageAsync(
        System.Linq.Expressions.Expression<Func<Post, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > 20)
            throw new ValidationException("Page must be positive and page size must be between 1 and 20.");

        var query = _context.Set<Post>().AsNoTracking().Where(predicate);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => Map(p))
            .ToListAsync(ct);

        return new PostPageDto(items, page, pageSize, totalCount);
    }

    private async Task<PostDto?> MapPostAsync(Guid postId, CancellationToken ct)
    {
        var post = await _context.Set<Post>()
            .AsNoTracking()
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
        return post is null ? null : Map(post);
    }

    private static PostDto Map(Post post) => new(
        post.Id,
        post.AuthorId,
        post.Author.Name,
        post.Author.Username,
        post.Author.ProfilePicture,
        post.CommunityId,
        post.Content,
        post.CreatedAt);
}

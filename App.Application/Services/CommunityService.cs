using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class CommunityService : ICommunityService
{
    private readonly DbContext _context;

    public CommunityService(DbContext context)
    {
        _context = context;
    }

    public async Task<CommunityDto> CreateAsync(Guid ownerId, CreateCommunityRequest request, CancellationToken ct = default)
    {
        var community = new Community(ownerId, request.Name, request.Description);
        _context.Set<Community>().Add(community);
        await _context.SaveChangesAsync(ct);

        var membership = new CommunityMember(community.Id, ownerId, MembershipRole.Owner);
        _context.Set<CommunityMember>().Add(membership);
        await _context.SaveChangesAsync(ct);

        var owner = await _context.Set<User>().FindAsync([ownerId], ct);

        return MapToDto(community, owner?.Name ?? string.Empty, 1);
    }

    public async Task<CommunityDto> UpdateAsync(Guid communityId, Guid currentUserId, UpdateCommunityRequest request, CancellationToken ct = default)
    {
        var community = await _context.Set<Community>()
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == communityId, ct);

        if (community is null)
            throw new CommunityNotFoundException(communityId);

        if (community.OwnerId != currentUserId)
            throw new UnauthorizedCommunityActionException();

        community.Update(request.Name, request.Description);
        await _context.SaveChangesAsync(ct);

        var memberCount = await GetMemberCountAsync(communityId, ct);

        return MapToDto(community, community.Owner.Name, memberCount);
    }

    public async Task<IReadOnlyList<CommunityDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var userExists = await _context.Set<User>()
            .AnyAsync(u => u.Id == userId && u.IsActive, ct);

        if (!userExists)
            throw new UserNotFoundException(userId.ToString());

        var memberships = await _context.Set<CommunityMember>()
            .AsNoTracking()
            .Include(m => m.Community)
                .ThenInclude(c => c.Owner)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.JoinedAt)
            .ThenBy(m => m.Community.Name)
            .ToListAsync(ct);

        var communityIds = memberships.Select(m => m.CommunityId).ToArray();
        var memberCounts = await _context.Set<CommunityMember>()
            .Where(m => communityIds.Contains(m.CommunityId))
            .GroupBy(m => m.CommunityId)
            .Select(g => new { CommunityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CommunityId, x => x.Count, ct);

        return memberships
            .Select(m => MapToDto(
                m.Community,
                m.Community.Owner.Name,
                memberCounts.GetValueOrDefault(m.CommunityId)))
            .ToList();
    }

    public async Task<IReadOnlyList<CommunitySearchResultDto>> SearchAsync(string term, int limit = 10, CancellationToken ct = default)
    {
        return (await SearchPageAsync(term, 1, limit, ct)).Items;
    }

    public async Task<SearchPageDto<CommunitySearchResultDto>> SearchPageAsync(
        string term,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var normalizedTerm = NormalizeSearchTerm(term);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var query = _context.Set<Community>()
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(normalizedTerm) ||
                (c.Description != null && c.Description.ToLower().Contains(normalizedTerm)));
        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommunitySearchResultDto(c.Id, c.Name, c.Description, c.Photo,
                _context.Set<CommunityMember>().Count(m => m.CommunityId == c.Id)))
            .ToListAsync(ct);

        return new SearchPageDto<CommunitySearchResultDto>(
            items, page, pageSize, totalItems, page * pageSize < totalItems);
    }

    public async Task<CommunityDto?> GetByIdAsync(Guid communityId, CancellationToken ct = default)
    {
        var community = await _context.Set<Community>()
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == communityId, ct);

        if (community is null)
            return null;

        var memberCount = await GetMemberCountAsync(communityId, ct);

        return MapToDto(community, community.Owner.Name, memberCount);
    }

    public async Task JoinAsync(Guid communityId, Guid userId, CancellationToken ct = default)
    {
        var community = await _context.Set<Community>()
            .FirstOrDefaultAsync(c => c.Id == communityId, ct);

        if (community is null)
            throw new CommunityNotFoundException(communityId);

        var alreadyMember = await _context.Set<CommunityMember>()
            .AnyAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);

        if (alreadyMember)
            throw new AlreadyCommunityMemberException();

        var membership = new CommunityMember(communityId, userId, MembershipRole.Member);
        _context.Set<CommunityMember>().Add(membership);
        await _context.SaveChangesAsync(ct);
    }

    public async Task LeaveAsync(Guid communityId, Guid userId, CancellationToken ct = default)
    {
        var community = await _context.Set<Community>()
            .FirstOrDefaultAsync(c => c.Id == communityId, ct);

        if (community is null)
            throw new CommunityNotFoundException(communityId);

        if (community.OwnerId == userId)
            throw new UnauthorizedCommunityActionException();

        var membership = await _context.Set<CommunityMember>()
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);

        if (membership is null)
            throw new NotCommunityMemberException();

        _context.Set<CommunityMember>().Remove(membership);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<CommunityMemberDto>> GetMembersAsync(Guid communityId, CancellationToken ct = default)
    {
        var communityExists = await _context.Set<Community>()
            .AnyAsync(c => c.Id == communityId, ct);

        if (!communityExists)
            throw new CommunityNotFoundException(communityId);

        var members = await _context.Set<CommunityMember>()
            .Include(m => m.User)
            .Where(m => m.CommunityId == communityId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

        return members.Select(MapToMemberDto);
    }

    public async Task<CommunityDto> SetPhotoAsync(Guid communityId, Guid currentUserId, string url, CancellationToken ct = default)
    {
        var community = await _context.Set<Community>()
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == communityId, ct);

        if (community is null)
            throw new CommunityNotFoundException(communityId);

        if (community.OwnerId != currentUserId)
            throw new UnauthorizedCommunityActionException();

        community.SetPhoto(url);
        await _context.SaveChangesAsync(ct);

        var memberCount = await GetMemberCountAsync(communityId, ct);

        return MapToDto(community, community.Owner.Name, memberCount);
    }

    public async Task<int> GetMemberCountAsync(Guid communityId, CancellationToken ct = default)
    {
        return await _context.Set<CommunityMember>()
            .CountAsync(m => m.CommunityId == communityId, ct);
    }

    private static CommunityDto MapToDto(Community community, string ownerName, int memberCount)
    {
        return new CommunityDto(
            community.Id,
            community.Name,
            community.Description,
            community.Photo,
            community.OwnerId,
            ownerName,
            memberCount,
            community.CreatedAt,
            community.UpdatedAt
        );
    }

    private static CommunityMemberDto MapToMemberDto(CommunityMember member)
    {
        return new CommunityMemberDto(
            member.CommunityId,
            member.UserId,
            member.User.Name,
            member.User.ProfilePicture,
            member.Role.ToString(),
            member.JoinedAt
        );
    }

    private static string NormalizeSearchTerm(string term)
    {
        var normalizedTerm = term?.Trim() ?? string.Empty;
        if (normalizedTerm.Length is < 2 or > 100)
            throw new App.Application.Exceptions.ValidationException("Search term must be between 2 and 100 characters.");

        return normalizedTerm.ToLowerInvariant();
    }
}

using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class ScrapService : IScrapService
{
    private readonly DbContext _context;

    public ScrapService(DbContext context)
    {
        _context = context;
    }

    public async Task<ScrapDto> CreateAsync(
        Guid authorId,
        CreateScrapRequest request,
        CancellationToken ct = default
    )
    {
        var recipient = await _context
            .Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.RecipientId && u.IsActive, ct);

        if (recipient is null)
            throw new UserNotFoundException(request.RecipientId.ToString());

        var visibility = ParseVisibility(request.Visibility);
        var scrap = new Scrap(authorId, recipient.Id, request.Content, visibility);
        _context.Set<Scrap>().Add(scrap);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(scrap).Reference(s => s.Author).LoadAsync(ct);
        await _context.Entry(scrap).Reference(s => s.Recipient).LoadAsync(ct);
        return MapToDto(scrap);
    }

    public async Task<ScrapPageDto> GetProfileScrapsAsync(
        Guid profileId,
        Guid viewerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default
    )
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context
            .Set<Scrap>()
            .Include(s => s.Author)
            .Include(s => s.Recipient)
            .Where(s =>
                s.RecipientId == profileId
                && (s.Visibility == ScrapVisibility.Public
                    || s.AuthorId == viewerId
                    || s.RecipientId == viewerId));

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ScrapPageDto(
            items.Select(MapToDto),
            page,
            pageSize,
            totalItems,
            page * pageSize < totalItems
        );
    }

    public async Task DeleteAsync(Guid scrapId, Guid authorId, CancellationToken ct = default)
    {
        var scrap = await _context.Set<Scrap>().FindAsync([scrapId], ct);
        if (scrap is null)
            throw new ScrapNotFoundException(scrapId);

        if (scrap.AuthorId != authorId)
            throw new UnauthorizedScrapActionException();

        _context.Set<Scrap>().Remove(scrap);
        await _context.SaveChangesAsync(ct);
    }

    private static ScrapVisibility ParseVisibility(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ScrapVisibility.Public;

        if (Enum.TryParse<ScrapVisibility>(value, true, out var visibility))
            return visibility;

        throw new ArgumentException("Scrap visibility must be Public or Private.", nameof(value));
    }

    private static ScrapDto MapToDto(Scrap scrap)
    {
        return new ScrapDto(
            scrap.Id,
            scrap.AuthorId,
            scrap.Author.Name,
            scrap.Author.Username,
            scrap.Author.ProfilePicture,
            scrap.RecipientId,
            scrap.Recipient.Name,
            scrap.Recipient.Username,
            scrap.Content,
            scrap.Visibility.ToString(),
            scrap.CreatedAt
        );
    }
}

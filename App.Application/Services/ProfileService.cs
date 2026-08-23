using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class ProfileService : IProfileService
{
    private readonly DbContext _context;
    private readonly IFriendshipService _friendshipService;

    public ProfileService(DbContext context, IFriendshipService friendshipService)
    {
        _context = context;
        _friendshipService = friendshipService;
    }

    public async Task<PublicProfileDto?> GetPublicProfileAsync(string username, CancellationToken ct = default)
    {
        var user = await _context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

        if (user is null)
            return null;

        var friendCount = await _friendshipService.GetFriendCountAsync(user.Id, ct);

        return new PublicProfileDto(
            user.Id,
            user.Name,
            user.Username,
            user.ProfilePicture,
            user.Bio,
            CalculateAge(user.BirthDate),
            user.City,
            user.State,
            user.RelationshipStatus?.ToString(),
            user.MusicInterests,
            user.MovieInterests,
            user.BookInterests,
            user.Hobbies,
            friendCount,
            user.CreatedAt
        );
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (birthDate is null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age)) age--;
        return age;
    }
}

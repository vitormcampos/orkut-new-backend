using App.Application.DTOs;
using App.Application.Exceptions;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class UserService : IUserService
{
    private readonly DbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(DbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        return user is null ? null : MapToDto(user);
    }

    public async Task<User?> GetUserEntityByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        return user is null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Set<User>()
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(MapToDto);
    }

    public async Task<IReadOnlyList<UserSearchResultDto>> SearchAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalizedTerm = NormalizeSearchTerm(term);
        var normalizedLimit = limit < 1 ? 10 : Math.Min(limit, 20);

        return await _context.Set<User>()
            .AsNoTracking()
            .Where(u => u.IsActive && (u.Name.ToLower().Contains(normalizedTerm) || u.Username.ToLower().Contains(normalizedTerm)))
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Take(normalizedLimit)
            .Select(u => new UserSearchResultDto(u.Id, u.Name, u.Username, u.ProfilePicture))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters long.");

        var email = request.Email.ToLowerInvariant();

        if (await _context.Set<User>().AnyAsync(u => u.Email == email, cancellationToken))
            throw new EmailAlreadyExistsException(email);

        if (await _context.Set<User>().AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new UsernameAlreadyTakenException(request.Username);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, email, request.Username, passwordHash);

        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters long.");

        user.SetName(request.Name);
        user.SetPasswordHash(_passwordHasher.Hash(request.Password));

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        user.SetName(request.Name);

        if (request.Username is not null && request.Username != user.Username)
        {
            if (await _context.Set<User>().AnyAsync(u => u.Username == request.Username && u.Id != id, cancellationToken))
                throw new UsernameAlreadyTakenException(request.Username);

            user.SetUsername(request.Username);
        }

        user.SetBio(request.Bio);

        user.SetBirthDate(request.BirthDate);
        user.SetLocation(request.City, request.State);
        user.SetRelationshipStatus(ParseRelationshipStatus(request.RelationshipStatus));
        user.SetInterests(request.MusicInterests, request.MovieInterests, request.BookInterests, request.Hobbies);

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        _context.Set<User>().Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        user.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> SetProfilePictureAsync(Guid id, string url, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        user.SetProfilePicture(url);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task SetLastLoginAsync(User user, CancellationToken cancellationToken = default)
    {
        user.SetLastLogin();
        _context.Set<User>().Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPasswordResetTokenAsync(User user, string token, CancellationToken cancellationToken = default)
    {
        user.SetPasswordResetToken(token);
        _context.Set<User>().Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);
    }

    public async Task ResetPasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default)
    {
        var hash = _passwordHasher.Hash(newPassword);
        user.SetPasswordHash(hash);
        user.ClearPasswordResetToken();
        _context.Set<User>().Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id, user.Name, user.Email, user.Username,
            user.ProfilePicture, user.Bio,
            user.IsActive, user.CreatedAt,
            user.BirthDate, user.City, user.State,
            user.RelationshipStatus?.ToString(),
            user.MusicInterests, user.MovieInterests, user.BookInterests, user.Hobbies
        );
    }

    private static RelationshipStatus? ParseRelationshipStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Enum.TryParse<RelationshipStatus>(value, true, out var status) || !Enum.IsDefined(status))
            throw new ValidationException($"Invalid relationship status: '{value}'.");
        return status;
    }

    private static string NormalizeSearchTerm(string term)
    {
        var normalizedTerm = term?.Trim() ?? string.Empty;
        if (normalizedTerm.Length is < 2 or > 100)
            throw new ValidationException("Search term must be between 2 and 100 characters.");

        return normalizedTerm.ToLowerInvariant();
    }
}

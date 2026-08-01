using App.Application.DTOs;
using App.Application.Exceptions;
using App.Application.Interfaces;
using App.Domain.Entities;
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

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters long.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, request.Email, passwordHash);

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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync([id], cancellationToken);

        if (user is null)
            throw new NotFoundException(nameof(User), id);

        _context.Set<User>().Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.CreatedAt
        );
    }
}

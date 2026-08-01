using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class AuthService : IAuthService
{
    private readonly DbContext _context;
    private readonly IUserService _userService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(
        DbContext context,
        IUserService userService,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _context = context;
        _userService = userService;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userDto = await _userService.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (userDto is null)
            throw new InvalidCredentialsException();

        var user = await _context.Set<User>().FindAsync([userDto.Id], cancellationToken);

        if (user is null || !user.IsActive)
            throw new AccountDeactivatedException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = await _tokenGenerator.GenerateRefreshTokenAsync(user.Id, cancellationToken);

        await _userService.SetLastLoginAsync(user, cancellationToken);

        return new LoginResponse(accessToken, refreshToken.Token, MapToDto(user));
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken is null || !storedToken.IsValid())
            throw new InvalidCredentialsException();

        storedToken.Revoke();
        _context.Set<RefreshToken>().Update(storedToken);

        var user = await _context.Set<User>().FindAsync([storedToken.UserId], cancellationToken);

        if (user is null || !user.IsActive)
            throw new AccountDeactivatedException();

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = await _tokenGenerator.GenerateRefreshTokenAsync(user.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, newRefreshToken.Token, MapToDto(user));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is not null)
        {
            storedToken.Revoke();
            _context.Set<RefreshToken>().Update(storedToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id, user.Name, user.Email, user.Username,
            user.ProfilePicture, user.Bio,
            user.IsActive, user.CreatedAt
        );
    }
}

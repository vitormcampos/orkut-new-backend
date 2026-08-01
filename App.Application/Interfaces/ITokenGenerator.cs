using App.Domain.Entities;

namespace App.Application.Interfaces;

public interface ITokenGenerator
{
    string GenerateAccessToken(User user);
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}

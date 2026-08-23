using App.Application.DTOs;

namespace App.Application.Interfaces;

public interface IProfileService
{
    Task<PublicProfileDto?> GetPublicProfileAsync(string username, CancellationToken ct = default);
}

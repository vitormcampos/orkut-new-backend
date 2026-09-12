using App.Application.DTOs;
using App.Domain.Entities;

namespace App.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSearchResultDto>> SearchAsync(string term, int limit = 10, CancellationToken cancellationToken = default);
    Task<SearchPageDto<UserSearchResultDto>> SearchPageAsync(string term, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> SetProfilePictureAsync(Guid id, string url, CancellationToken cancellationToken = default);
    Task SetLastLoginAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserEntityByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task SetPasswordResetTokenAsync(User user, string token, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default);
}

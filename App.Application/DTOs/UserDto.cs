namespace App.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Username,
    string? ProfilePicture,
    string? Bio,
    bool IsActive,
    DateTime CreatedAt,
    DateOnly? BirthDate,
    string? City,
    string? State,
    string? RelationshipStatus,
    string[]? MusicInterests,
    string[]? MovieInterests,
    string[]? BookInterests,
    string[]? Hobbies
);

public record PublicProfileDto(
    Guid Id,
    string Name,
    string Username,
    string? ProfilePicture,
    string? Bio,
    int? Age,
    string? City,
    string? State,
    string? RelationshipStatus,
    string[]? MusicInterests,
    string[]? MovieInterests,
    string[]? BookInterests,
    string[]? Hobbies,
    int FriendCount,
    DateTime CreatedAt
);

public record UserSummaryDto(
    Guid Id,
    string Name,
    string Username,
    string? ProfilePicture
);

public record CreateUserRequest(string Name, string Email, string Username, string Password);

public record UpdateUserRequest(string Name, string Password);

public record UpdateProfileRequest(
    string Name,
    string? Username,
    string? Bio,
    DateOnly? BirthDate,
    string? City,
    string? State,
    string? RelationshipStatus,
    string[]? MusicInterests,
    string[]? MovieInterests,
    string[]? BookInterests,
    string[]? Hobbies
);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken, UserDto User);

public record RefreshTokenRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

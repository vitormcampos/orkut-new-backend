namespace App.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt
);

public record CreateUserRequest(string Name, string Email, string Password);

public record UpdateUserRequest(string Name, string Password);

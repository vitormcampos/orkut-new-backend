namespace App.Application.DTOs;

public record FriendshipDto(
    Guid Id,
    Guid RequesterId,
    string RequesterName,
    string RequesterUsername,
    string? RequesterProfilePicture,
    Guid AddresseeId,
    string AddresseeName,
    string AddresseeUsername,
    string? AddresseeProfilePicture,
    string Status,
    DateTime RequestedAt,
    DateTime? RespondedAt
);

public record SendFriendshipRequest(string AddresseeUsername);

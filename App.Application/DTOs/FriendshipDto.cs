namespace App.Application.DTOs;

public record FriendshipDto(
    Guid Id,
    Guid RequesterId,
    string RequesterName,
    string RequesterUsername,
    Guid AddresseeId,
    string AddresseeName,
    string AddresseeUsername,
    string Status,
    DateTime RequestedAt,
    DateTime? RespondedAt
);

public record SendFriendshipRequest(string AddresseeUsername);

using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId, ct);

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userService.UpdateProfileAsync(userId, request, ct);

        return Ok(user);
    }

    [HttpPost("me/photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();

        // Simple local file storage
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "photos");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{userId}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        var photoUrl = $"/uploads/photos/{fileName}";
        var user = await _userService.SetProfilePictureAsync(userId, photoUrl, ct);

        return Ok(user);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateAccount(CancellationToken ct)
    {
        var userId = GetUserId();
        await _userService.DeactivateAsync(userId, ct);

        return NoContent();
    }
}

using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class PasswordRecoveryController : ControllerBase
{
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordRecoveryController(IPasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    [HttpPost("forgot")]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _passwordRecoveryService.SendResetTokenAsync(request.Email, ct);
        return NoContent();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _passwordRecoveryService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        return NoContent();
    }
}

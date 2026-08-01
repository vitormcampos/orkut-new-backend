using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints publicos para recuperacao de senha.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PasswordRecoveryController : ControllerBase
{
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordRecoveryController(IPasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    /// <summary>
    /// Envia um token de redefinicao de senha para o email informado.
    /// </summary>
    /// <param name="request">Email da conta que solicitou recuperacao.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Solicitacao processada com sucesso.</response>
    /// <response code="400">Email invalido.</response>
    [HttpPost("forgot")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _passwordRecoveryService.SendResetTokenAsync(request.Email, ct);
        return NoContent();
    }

    /// <summary>
    /// Redefine a senha usando um token de recuperacao valido.
    /// </summary>
    /// <param name="request">Token de recuperacao e nova senha.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Senha redefinida com sucesso.</response>
    /// <response code="400">Token ou senha invalidos.</response>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _passwordRecoveryService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        return NoContent();
    }
}

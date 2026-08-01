using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints de autenticacao, cadastro e renovacao de tokens.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// Cria uma nova conta de usuario.
    /// </summary>
    /// <param name="request">Dados de cadastro do usuario.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Usuario criado.</returns>
    /// <response code="201">Conta criada com sucesso.</response>
    /// <response code="400">Dados invalidos ou email/username indisponivel.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
    }

    /// <summary>
    /// Autentica o usuario e retorna tokens de acesso e renovacao.
    /// </summary>
    /// <param name="request">Credenciais do usuario.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Tokens JWT e dados do usuario autenticado.</returns>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="401">Credenciais invalidas.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Renova o token de acesso usando um refresh token valido.
    /// </summary>
    /// <param name="request">Refresh token emitido anteriormente.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Novos tokens JWT e dados do usuario.</returns>
    /// <response code="200">Token renovado com sucesso.</response>
    /// <response code="401">Refresh token invalido, expirado ou revogado.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshTokenAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Revoga o refresh token informado e encerra a sessao.
    /// </summary>
    /// <param name="request">Refresh token que deve ser revogado.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Logout realizado com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}

using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints protegidos para consulta do perfil publico de outros usuarios.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfilesController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Retorna o perfil publico de um usuario pelo username.
    /// </summary>
    /// <param name="username">Username do usuario cujo perfil publico deve ser retornado.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil publico do usuario, incluindo contagem de amigos e idade.</returns>
    /// <response code="200">Perfil publico retornado com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Usuario nao encontrado ou inativo.</response>
    [HttpGet("{username}")]
    [ProducesResponseType(typeof(PublicProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(string username, CancellationToken ct)
    {
        var profile = await _profileService.GetPublicProfileAsync(username, ct);

        if (profile is null)
            return NotFound();

        return Ok(profile);
    }
}

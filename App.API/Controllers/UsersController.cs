using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints protegidos para consulta e manutencao do perfil do usuario autenticado.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IStorageService _storageService;

    public UsersController(IUserService userService, IStorageService storageService)
    {
        _userService = userService;
        _storageService = storageService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>
    /// Retorna o perfil do usuario autenticado.
    /// </summary>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Dados do usuario autenticado.</returns>
    /// <response code="200">Perfil retornado com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Usuario nao encontrado.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId, ct);

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Atualiza dados basicos do perfil do usuario autenticado.
    /// </summary>
    /// <param name="request">Dados que devem ser atualizados no perfil.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil atualizado.</returns>
    /// <response code="200">Perfil atualizado com sucesso.</response>
    /// <response code="400">Dados invalidos ou username indisponivel.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userService.UpdateProfileAsync(userId, request, ct);

        return Ok(user);
    }

    /// <summary>
    /// Envia uma nova foto de perfil para o usuario autenticado.
    /// </summary>
    /// <param name="file">Arquivo da foto de perfil.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil atualizado com a URL da foto.</returns>
    /// <response code="200">Foto enviada com sucesso.</response>
    /// <response code="400">Arquivo ausente ou invalido.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpPost("me/photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var key = $"{userId}/photos/{fileName}";
        var photoUrl = await _storageService.UploadAsync(file.OpenReadStream(), key, file.ContentType, ct);

        var user = await _userService.SetProfilePictureAsync(userId, photoUrl, ct);

        return Ok(user);
    }

    /// <summary>
    /// Desativa a conta do usuario autenticado.
    /// </summary>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Conta desativada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateAccount(CancellationToken ct)
    {
        var userId = GetUserId();
        await _userService.DeactivateAsync(userId, ct);

        return NoContent();
    }
}

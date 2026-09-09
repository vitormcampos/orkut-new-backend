using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly ProfilePhotoOptions _profilePhotoOptions;

    public UsersController(
        IUserService userService,
        IStorageService storageService,
        IOptions<ProfilePhotoOptions> profilePhotoOptions)
    {
        _userService = userService;
        _storageService = storageService;
        _profilePhotoOptions = profilePhotoOptions.Value;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>Busca usuarios ativos por nome ou username.</summary>
    /// <param name="term">Termo entre 2 e 100 caracteres.</param>
    /// <param name="limit">Padrao 10; valores menores que 1 usam 10 e valores acima de 20 sao limitados a 20.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Usuarios encontrados, ordenados por nome e id.</returns>
    /// <response code="200">Busca concluida.</response>
    /// <response code="400">Termo invalido.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        return Ok(await _userService.SearchAsync(term, limit, ct));
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
    public async Task<IActionResult> UploadPhoto(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("A foto de perfil e obrigatoria.");

        if (file.Length > _profilePhotoOptions.MaxFileSizeBytes)
            return BadRequest("A foto de perfil deve ter no maximo 5 MB.");

        if (!_profilePhotoOptions.AllowedContentTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
            return BadRequest("Formato de foto nao permitido. Use JPEG, PNG ou WebP.");

        var userId = GetUserId();
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var key = $"{userId}/photos/{fileName}";

        await using var stream = file.OpenReadStream();
        var photoUrl = await _storageService.UploadAsync(stream, key, file.ContentType, ct);
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

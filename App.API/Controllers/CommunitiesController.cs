using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints protegidos para gerenciamento de comunidades.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class CommunitiesController : ControllerBase
{
    private readonly ICommunityService _communityService;
    private readonly IStorageService _storageService;

    public CommunitiesController(ICommunityService communityService, IStorageService storageService)
    {
        _communityService = communityService;
        _storageService = storageService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>Busca comunidades por nome ou descricao.</summary>
    /// <param name="term">Termo entre 2 e 100 caracteres.</param>
    /// <param name="limit">Padrao 10; valores menores que 1 usam 10 e valores acima de 20 sao limitados a 20.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Comunidades encontradas, ordenadas por nome e id.</returns>
    /// <response code="200">Busca concluida.</response>
    /// <response code="400">Termo invalido.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<CommunitySearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        return Ok(await _communityService.SearchAsync(term, limit, ct));
    }

    /// <summary>
    /// Cria uma nova comunidade, definindo o usuario autenticado como dono.
    /// </summary>
    /// <param name="request">Dados da comunidade a ser criada.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Comunidade criada.</returns>
    /// <response code="201">Comunidade criada com sucesso.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _communityService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetCommunity), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza nome e descricao de uma comunidade.
    /// </summary>
    /// <param name="id">Id da comunidade a ser atualizada.</param>
    /// <param name="request">Dados atualizados da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Comunidade atualizada.</returns>
    /// <response code="200">Comunidade atualizada com sucesso.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="403">Usuario nao e o dono da comunidade.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCommunity(Guid id, [FromBody] UpdateCommunityRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _communityService.UpdateAsync(id, userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retorna os dados de uma comunidade pelo Id.
    /// </summary>
    /// <param name="id">Id da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Dados da comunidade.</returns>
    /// <response code="200">Comunidade retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCommunity(Guid id, CancellationToken ct)
    {
        var community = await _communityService.GetByIdAsync(id, ct);

        if (community is null)
            return NotFound();

        return Ok(community);
    }

    /// <summary>
    /// Lista os membros de uma comunidade.
    /// </summary>
    /// <param name="id">Id da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de membros da comunidade.</returns>
    /// <response code="200">Membros listados com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<CommunityMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct)
    {
        var members = await _communityService.GetMembersAsync(id, ct);
        return Ok(members);
    }

    /// <summary>
    /// Faz o usuario autenticado ingressar em uma comunidade.
    /// </summary>
    /// <param name="id">Id da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Usuario ingressou na comunidade com sucesso.</response>
    /// <response code="400">Usuario ja e membro da comunidade.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinCommunity(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _communityService.JoinAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Faz o usuario autenticado sair de uma comunidade.
    /// </summary>
    /// <param name="id">Id da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Usuario saiu da comunidade com sucesso.</response>
    /// <response code="400">Usuario nao e membro da comunidade.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="403">Dono nao pode sair da propria comunidade.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveCommunity(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _communityService.LeaveAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Envia uma nova foto para a comunidade.
    /// </summary>
    /// <param name="id">Id da comunidade.</param>
    /// <param name="file">Arquivo da foto da comunidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Comunidade atualizada com a URL da foto.</returns>
    /// <response code="200">Foto enviada com sucesso.</response>
    /// <response code="400">Arquivo ausente ou invalido.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="403">Usuario nao e o dono da comunidade.</response>
    /// <response code="404">Comunidade nao encontrada.</response>
    [HttpPost("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CommunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var key = $"communities/{id}/photos/{fileName}";
        var photoUrl = await _storageService.UploadAsync(file.OpenReadStream(), key, file.ContentType, ct);

        var result = await _communityService.SetPhotoAsync(id, userId, photoUrl, ct);

        return Ok(result);
    }
}

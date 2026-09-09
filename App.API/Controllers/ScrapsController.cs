using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints protegidos para gerenciamento de scraps.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ScrapsController : ControllerBase
{
    private readonly IScrapService _scrapService;

    public ScrapsController(IScrapService scrapService)
    {
        _scrapService = scrapService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>
    /// Publica um scrap no perfil de outro usuario.
    /// </summary>
    /// <param name="request">Dados do destinatario, conteudo e visibilidade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Scrap publicado.</returns>
    /// <response code="201">Scrap publicado com sucesso.</response>
    /// <response code="400">Dados invalidos ou tentativa de publicar para si mesmo.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Destinatario nao encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ScrapDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateScrapRequest request, CancellationToken ct)
    {
        var result = await _scrapService.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lista os scraps visiveis no perfil de um usuario.
    /// </summary>
    /// <param name="profileId">Id do usuario cujo perfil sera consultado.</param>
    /// <param name="page">Numero da pagina, iniciando em 1.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Scraps visiveis e informacoes de paginacao.</returns>
    /// <response code="200">Scraps listados com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet("profile/{profileId:guid}")]
    [ProducesResponseType(typeof(ScrapPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileScraps(
        Guid profileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _scrapService.GetProfileScrapsAsync(profileId, GetUserId(), page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exclui um scrap publicado pelo usuario autenticado.
    /// </summary>
    /// <param name="id">Id do scrap.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Scrap excluido com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="403">Usuario nao e o autor do scrap.</response>
    /// <response code="404">Scrap nao encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _scrapService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }
}

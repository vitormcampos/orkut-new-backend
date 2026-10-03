using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>Consulta e publicação de posts pessoais e comunitários.</summary>
[ApiController]
[Route("[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService) => _postService = postService;

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>Cria um post pessoal ou em uma comunidade da qual o autor participa.</summary>
    /// <param name="request">Conteúdo e comunidade opcional do post.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Post criado.</returns>
    /// <response code="201">Post criado.</response>
    /// <response code="400">Conteúdo inválido ou usuário não é membro da comunidade.</response>
    /// <response code="401">Autenticação necessária.</response>
    /// <response code="404">Comunidade não encontrada.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePostRequest request,
        CancellationToken ct
    )
    {
        var post = await _postService.CreateAsync(GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    /// <summary>Lista posts pessoais públicos de um usuário.</summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="page">Página iniciando em 1.</param>
    /// <param name="pageSize">Itens por página, entre 1 e 20.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Posts pessoais paginados.</returns>
    /// <response code="200">Posts listados.</response>
    /// <response code="400">Paginação inválida.</response>
    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default
    ) => Ok(await _postService.GetByUserAsync(userId, page, pageSize, ct));

    /// <summary>Lista posts públicos de uma comunidade.</summary>
    /// <param name="communityId">Identificador da comunidade.</param>
    /// <param name="page">Página iniciando em 1.</param>
    /// <param name="pageSize">Itens por página, entre 1 e 20.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Posts da comunidade paginados.</returns>
    /// <response code="200">Posts listados.</response>
    /// <response code="400">Paginação inválida.</response>
    [HttpGet("communities/{communityId:guid}")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCommunity(
        Guid communityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default
    ) => Ok(await _postService.GetByCommunityAsync(communityId, page, pageSize, ct));

    /// <summary>Consulta um post público pelo identificador.</summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Post encontrado.</returns>
    /// <response code="200">Post retornado.</response>
    /// <response code="404">Post não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var post = await _postService.GetByIdAsync(id, ct);
        return post is null ? NotFound() : Ok(post);
    }

    /// <summary>Exclui um post próprio ou, se autorizado, modera post da comunidade.</summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="204">Post excluído.</response>
    /// <response code="401">Autenticação necessária.</response>
    /// <response code="403">Sem permissão para excluir.</response>
    /// <response code="404">Post não encontrado.</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _postService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }
}

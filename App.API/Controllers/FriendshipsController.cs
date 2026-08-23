using System.Security.Claims;
using App.Application.DTOs;
using App.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Endpoints protegidos para gerenciamento de amizades.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class FriendshipsController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;

    public FriendshipsController(IFriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    /// <summary>
    /// Envia uma solicitacao de amizade para outro usuario.
    /// </summary>
    /// <param name="request">Dados do destinatario da solicitacao.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Solicitacao de amizade criada.</returns>
    /// <response code="201">Solicitacao enviada com sucesso.</response>
    /// <response code="400">Tentativa de enviar solicitacao para si mesmo ou usuarios ja amigos.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Usuario destinatario nao encontrado.</response>
    [HttpPost("request")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendshipRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _friendshipService.SendRequestAsync(userId, request.AddresseeUsername, ct);
        return CreatedAtAction(nameof(SendRequest), new { id = result.Id }, result);
    }

    /// <summary>
    /// Aceita uma solicitacao de amizade pendente.
    /// </summary>
    /// <param name="id">Id da solicitacao de amizade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Solicitacao de amizade atualizada.</returns>
    /// <response code="200">Solicitacao aceita com sucesso.</response>
    /// <response code="400">Solicitacao nao esta pendente ou usuario nao e o destinatario.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Solicitacao nao encontrada.</response>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _friendshipService.AcceptRequestAsync(id, userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Recusa uma solicitacao de amizade pendente.
    /// </summary>
    /// <param name="id">Id da solicitacao de amizade.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Solicitacao de amizade atualizada.</returns>
    /// <response code="200">Solicitacao recusada com sucesso.</response>
    /// <response code="400">Solicitacao nao esta pendente ou usuario nao e o destinatario.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Solicitacao nao encontrada.</response>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _friendshipService.RejectRequestAsync(id, userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Remove uma amizade existente.
    /// </summary>
    /// <param name="id">Id da amizade a ser removida.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <response code="204">Amizade removida com sucesso.</response>
    /// <response code="400">Usuario nao e participante da amizade.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Amizade nao encontrada.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriendship(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _friendshipService.RemoveFriendshipAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Lista os amigos do usuario autenticado.
    /// </summary>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de amizades aceitas do usuario.</returns>
    /// <response code="200">Amigos listados com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        var userId = GetUserId();
        var friends = await _friendshipService.GetFriendsAsync(userId, ct);
        return Ok(friends);
    }

    /// <summary>
    /// Lista as solicitacoes de amizade pendentes recebidas pelo usuario autenticado.
    /// </summary>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de solicitacoes pendentes recebidas.</returns>
    /// <response code="200">Solicitacoes listadas com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var userId = GetUserId();
        var pending = await _friendshipService.GetPendingRequestsAsync(userId, ct);
        return Ok(pending);
    }

    /// <summary>
    /// Lista as solicitacoes de amizade pendentes enviadas pelo usuario autenticado.
    /// </summary>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de solicitacoes pendentes enviadas.</returns>
    /// <response code="200">Solicitacoes listadas com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet("sent")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSentRequests(CancellationToken ct)
    {
        var userId = GetUserId();
        var sent = await _friendshipService.GetSentRequestsAsync(userId, ct);
        return Ok(sent);
    }

    /// <summary>
    /// Retorna os amigos em comum entre o usuario autenticado e outro usuario.
    /// </summary>
    /// <param name="userId">Id do outro usuario para comparacao.</param>
    /// <param name="ct">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de usuarios que sao amigos em comum.</returns>
    /// <response code="200">Amigos em comum listados com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet("in-common/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriendsInCommon(Guid userId, CancellationToken ct)
    {
        var currentUserId = GetUserId();
        var common = await _friendshipService.GetFriendsInCommonAsync(currentUserId, userId, ct);
        return Ok(common);
    }
}

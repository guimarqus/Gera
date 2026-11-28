using System.Security.Claims;
using Gera.Application.Mensagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gera.Api.Hubs;

[Authorize]
public sealed class ChatHub(MensagemService mensagens) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.User?.FindFirstValue("sub"), out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoUsuario(userId));
        await base.OnConnectedAsync();
    }

    public async Task EntrarSolicitacao(int solicitacaoId)
    {
        var userId = UsuarioId();
        if (!await mensagens.PodeAcessarAsync(userId, solicitacaoId, Context.ConnectionAborted))
            throw new HubException("Solicitação não encontrada.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoSolicitacao(solicitacaoId), Context.ConnectionAborted);
    }

    public async Task Digitar(int solicitacaoId)
    {
        var userId = UsuarioId();
        if (!await mensagens.PodeAcessarAsync(userId, solicitacaoId, Context.ConnectionAborted))
            throw new HubException("Solicitação não encontrada.");
        await Clients.OthersInGroup(GrupoSolicitacao(solicitacaoId)).SendAsync("UsuarioDigitando", userId, Context.User?.FindFirst("name")?.Value ?? "Participante", Context.ConnectionAborted);
    }

    public static string GrupoSolicitacao(int id) => $"solicitacao:{id}";
    public static string GrupoUsuario(int id) => $"usuario:{id}";

    private int UsuarioId() => int.TryParse(Context.User?.FindFirstValue("sub"), out var id)
        ? id : throw new HubException("Sessão inválida.");
}

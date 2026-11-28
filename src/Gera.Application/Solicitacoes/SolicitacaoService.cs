using System.Data;
using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Solicitacoes;

public sealed class SolicitacaoService(IAppDbContext db)
{
    public async Task<SolicitacaoDto> CriarAsync(int clienteId, int ofertaId, CriarSolicitacaoRequest request, CancellationToken ct)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var oferta = await db.Ofertas.AsNoTracking().SingleOrDefaultAsync(o => o.Id == ofertaId && o.Status == StatusOferta.Ativo && o.ExcluidoEm == null, ct)
            ?? throw new NotFoundException("Oferta", ofertaId);
        if (oferta.PrestadorId == clienteId) throw new DomainException("Não é possível solicitar o próprio serviço.");
        var descricao = request.Descricao?.Trim() ?? "";
        if (descricao.Length is < 10 or > 2000) throw new DomainException("A descrição deve ter entre 10 e 2.000 caracteres.");
        var item = new Solicitacao { ClienteId = clienteId, PrestadorId = oferta.PrestadorId, OfertaId = ofertaId, Descricao = descricao };
        db.Solicitacoes.Add(item);
        await db.SaveChangesAsync(ct);
        await NotificarAsync(oferta.PrestadorId, "Nova solicitação", "Você recebeu um pedido de orçamento.", TipoNotificacao.NovoPedido, item.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await ObterAsync(clienteId, item.Id, ct);
    }

    public async Task<IReadOnlyList<SolicitacaoDto>> MinhasAsync(int usuarioId, EtapaKanban? etapa, CancellationToken ct)
    {
        var itens = await Base().Where(s => (s.ClienteId == usuarioId || s.PrestadorId == usuarioId) && (!etapa.HasValue || s.Etapa == etapa))
            .OrderByDescending(s => s.AtualizadoEm).Take(100).ToListAsync(ct);
        return itens.Select(Map).ToArray();
    }

    public async Task<SolicitacaoDto> ObterAsync(int usuarioId, int id, CancellationToken ct)
    {
        var item = await Base().SingleOrDefaultAsync(s => s.Id == id && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId), ct)
            ?? throw new NotFoundException("Solicitação", id);
        return Map(item);
    }

    public async Task<SolicitacaoDto> ProporAsync(int usuarioId, int id, ProporOrcamentoRequest request, CancellationToken ct)
    {
        var item = await ObterEntidadeAsync(usuarioId, id, request.Versao, ct);
        if (item.Etapa is not (EtapaKanban.Solicitacoes or EtapaKanban.EmNegociacao)) throw new DomainException("Só é possível negociar uma solicitação aberta.");
        if (request.Valor <= 0 || request.Valor > 10000000 || request.PrazoEmDias is <= 0 or > 3650 ||
            string.IsNullOrWhiteSpace(request.OQueEstaIncluso) || request.OQueEstaIncluso.Trim().Length > 1000 ||
            request.Observacoes?.Length > 1000 || (request.DataPrevisao.HasValue && request.DataPrevisao.Value <= DateTime.UtcNow))
            throw new DomainException("Confira o valor, prazo, data e descrição do orçamento.");
        var pendentes = await db.Orcamentos.Where(o => o.SolicitacaoId == id && o.Status == StatusOrcamento.Pendente).ToListAsync(ct);
        foreach (var anterior in pendentes) { anterior.Status = StatusOrcamento.Recusado; anterior.RespondidoEm = DateTime.UtcNow; }
        var orcamento = new Orcamento
        {
            SolicitacaoId = id, PrestadorId = item.PrestadorId, AutorId = usuarioId, Valor = request.Valor,
            PrazoEmDias = request.PrazoEmDias, DataPrevisao = request.DataPrevisao,
            OQueEstaIncluso = request.OQueEstaIncluso.Trim(), Observacoes = request.Observacoes?.Trim(), Status = StatusOrcamento.Pendente
        };
        db.Orcamentos.Add(orcamento);
        db.Mensagens.Add(new Mensagem { SolicitacaoId = id, RemetenteId = usuarioId, Tipo = TipoMensagem.OrcamentoProposto,
            Conteudo = $"Orçamento proposto: {orcamento.Valor.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))} — {orcamento.OQueEstaIncluso}", Orcamento = orcamento });
        await AvancarEtapaAsync(item, usuarioId, EtapaKanban.EmNegociacao, "Orçamento proposto", ct);
        await NotificarAsync(usuarioId == item.ClienteId ? item.PrestadorId : item.ClienteId, "Novo orçamento", "Uma nova proposta foi enviada.", TipoNotificacao.OrcamentoRecebido, id);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(usuarioId, id, ct);
    }

    public async Task<SolicitacaoDto> ResponderOrcamentoAsync(int usuarioId, int id, int orcamentoId, bool aceitar, string versao, CancellationToken ct)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var item = await ObterEntidadeAsync(usuarioId, id, versao, ct);
        var quote = await db.Orcamentos.SingleOrDefaultAsync(o => o.Id == orcamentoId && o.SolicitacaoId == id && o.Status == StatusOrcamento.Pendente, ct)
            ?? throw new NotFoundException("Orçamento", orcamentoId);
        if (quote.AutorId == usuarioId) throw new DomainException("A proposta deve ser respondida pela outra parte.");
        quote.RespondidoEm = DateTime.UtcNow;
        quote.Status = aceitar ? StatusOrcamento.Aceito : StatusOrcamento.Recusado;
        db.Mensagens.Add(new Mensagem { SolicitacaoId = id, RemetenteId = usuarioId, Tipo = TipoMensagem.OrcamentoProposto,
            Conteudo = aceitar ? "Orçamento aceito e serviço agendado." : "Orçamento recusado.", OrcamentoId = quote.Id });
        if (aceitar)
        {
            if (!quote.DataPrevisao.HasValue) throw new DomainException("A proposta precisa informar uma data para ser aceita e agendada.");
            item.Valor = quote.Valor;
            item.DataAgendada = quote.DataPrevisao;
            item.AceiteCliente = item.AceitePrestador = true;
            await AvancarEtapaAsync(item, usuarioId, EtapaKanban.Agendados, "Orçamento aceito pelas partes", ct);
        }
        await NotificarAsync(quote.AutorId, aceitar ? "Orçamento aceito" : "Orçamento recusado", aceitar ? "A proposta foi aceita e o serviço está agendado." : "A proposta foi recusada.", aceitar ? TipoNotificacao.OrcamentoAceito : TipoNotificacao.OrcamentoRecusado, id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await ObterAsync(usuarioId, id, ct);
    }

    public async Task<SolicitacaoDto> MoverAsync(int usuarioId, int id, MoverSolicitacaoRequest request, CancellationToken ct)
    {
        var item = await ObterEntidadeAsync(usuarioId, id, request.Versao, ct);
        if (request.Etapa == EtapaKanban.EmExecucao)
        {
            if (usuarioId != item.PrestadorId || item.Etapa != EtapaKanban.Agendados) throw new DomainException("Somente o prestador pode iniciar um serviço agendado.");
            await AvancarEtapaAsync(item, usuarioId, request.Etapa, null, ct);
        }
        else if (request.Etapa == EtapaKanban.Cancelado)
        {
            if (item.Etapa is not (EtapaKanban.Solicitacoes or EtapaKanban.EmNegociacao or EtapaKanban.Agendados)) throw new DomainException("Este serviço não pode mais ser cancelado.");
            var motivo = request.Motivo?.Trim();
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 500) throw new DomainException("Informe um motivo de cancelamento (até 500 caracteres).");
            item.MotivoCancelamento = motivo; item.CanceladoPorId = usuarioId;
            var propostasPendentes = await db.Orcamentos.Where(o => o.SolicitacaoId == id && o.Status == StatusOrcamento.Pendente).ToListAsync(ct);
            foreach (var proposta in propostasPendentes) { proposta.Status = StatusOrcamento.Expirado; proposta.RespondidoEm = DateTime.UtcNow; }
            await AvancarEtapaAsync(item, usuarioId, request.Etapa, motivo, ct);
        }
        else throw new DomainException("Transição solicitada não permitida.");
        await db.SaveChangesAsync(ct);
        return await ObterAsync(usuarioId, id, ct);
    }

    public async Task<SolicitacaoDto> SolicitarConclusaoAsync(int usuarioId, int id, AcaoSolicitacaoRequest request, CancellationToken ct)
    {
        var item = await ObterEntidadeAsync(usuarioId, id, request.Versao, ct);
        if (usuarioId != item.PrestadorId || item.Etapa != EtapaKanban.EmExecucao) throw new DomainException("Somente o prestador pode solicitar a conclusão durante a execução.");
        item.ConcluidoPorPrestador = true;
        db.Mensagens.Add(new Mensagem { SolicitacaoId = id, RemetenteId = usuarioId, Tipo = TipoMensagem.MudancaEtapa, Conteudo = "O prestador solicitou a confirmação de conclusão." });
        await NotificarAsync(item.ClienteId, "Serviço concluído?", "O prestador solicitou a confirmação da conclusão.", TipoNotificacao.EtapaAlterada, id);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(usuarioId, id, ct);
    }

    public async Task<SolicitacaoDto> ConfirmarConclusaoAsync(int usuarioId, int id, AcaoSolicitacaoRequest request, CancellationToken ct)
    {
        var item = await ObterEntidadeAsync(usuarioId, id, request.Versao, ct);
        if (usuarioId != item.ClienteId || item.Etapa != EtapaKanban.EmExecucao || !item.ConcluidoPorPrestador)
            throw new DomainException("O cliente só pode confirmar após o prestador solicitar a conclusão.");
        item.ConcluidoPorCliente = true;
        await AvancarEtapaAsync(item, usuarioId, EtapaKanban.Concluidos, "Conclusão confirmada pelo cliente", ct);
        await NotificarAsync(item.PrestadorId, "Serviço concluído", "O cliente confirmou a conclusão do serviço.", TipoNotificacao.EtapaAlterada, id);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(usuarioId, id, ct);
    }

    private IQueryable<Solicitacao> Base() => db.Solicitacoes.AsNoTracking()
        .Include(s => s.Cliente).Include(s => s.Prestador).Include(s => s.Oferta)
        .Include(s => s.Orcamentos.OrderByDescending(o => o.CriadoEm)).Include(s => s.Historicos.OrderBy(h => h.OcorreuEm));

    private static SolicitacaoDto Map(Solicitacao s) => new(s.Id, s.ClienteId, s.Cliente.Nome, s.PrestadorId, s.Prestador.Nome, s.OfertaId, s.Oferta.Titulo, s.Descricao, s.Etapa, s.Valor, s.DataAgendada, s.ConcluidoPorPrestador, s.ConcluidoPorCliente, Convert.ToBase64String(s.RowVersion), s.CriadoEm, s.MotivoCancelamento,
        s.Orcamentos.Select(o => new OrcamentoDto(o.Id, o.AutorId, o.Valor, o.PrazoEmDias, o.DataPrevisao, o.OQueEstaIncluso, o.Observacoes, o.Status, o.CriadoEm, o.RespondidoEm)).ToArray(),
        s.Historicos.Select(h => new HistoricoSolicitacaoDto(h.UsuarioId, h.EtapaAnterior, h.EtapaNova, h.Observacao, h.OcorreuEm)).ToArray());

    private async Task<Solicitacao> ObterEntidadeAsync(int usuarioId, int id, string versao, CancellationToken ct)
    {
        var item = await db.Solicitacoes.SingleOrDefaultAsync(s => s.Id == id && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId), ct)
            ?? throw new NotFoundException("Solicitação", id);
        byte[] original;
        try { original = Convert.FromBase64String(versao); } catch (FormatException) { throw new DomainException("Versão da solicitação inválida. Atualize os dados."); }
        if (original.Length != 8) throw new DomainException("Versão da solicitação inválida. Atualize os dados.");
        db.Solicitacoes.Entry(item).Property(s => s.RowVersion).OriginalValue = original;
        item.AtualizadoEm = DateTime.UtcNow;
        return item;
    }

    private async Task AvancarEtapaAsync(Solicitacao item, int usuarioId, EtapaKanban destino, string? observacao, CancellationToken ct)
    {
        var origem = item.Etapa;
        if (origem == destino) return;
        var permitido = (origem, destino) switch
        {
            (EtapaKanban.Solicitacoes, EtapaKanban.EmNegociacao) => true,
            (EtapaKanban.EmNegociacao, EtapaKanban.EmNegociacao) => true,
            (EtapaKanban.EmNegociacao, EtapaKanban.Agendados) => true,
            (EtapaKanban.Agendados, EtapaKanban.EmExecucao) => true,
            (EtapaKanban.EmExecucao, EtapaKanban.Concluidos) => true,
            (EtapaKanban.Solicitacoes or EtapaKanban.EmNegociacao or EtapaKanban.Agendados, EtapaKanban.Cancelado) => true,
            _ => false
        };
        if (!permitido) throw new TransicaoInvalidaException(origem, destino, "A etapa não pode ser avançada neste momento.");
        item.Etapa = destino;
        db.SolicitacaoHistoricos.Add(new SolicitacaoHistorico { SolicitacaoId = item.Id, UsuarioId = usuarioId, EtapaAnterior = origem, EtapaNova = destino, Observacao = observacao });
        db.Mensagens.Add(new Mensagem { SolicitacaoId = item.Id, RemetenteId = usuarioId, Tipo = TipoMensagem.MudancaEtapa, Conteudo = $"Solicitação avançou para {destino}." });
        var outro = usuarioId == item.ClienteId ? item.PrestadorId : item.ClienteId;
        await NotificarAsync(outro, "Atualização da solicitação", $"A solicitação avançou para {destino}.", TipoNotificacao.EtapaAlterada, item.Id);
    }

    private async Task NotificarAsync(int usuarioId, string titulo, string mensagem, TipoNotificacao tipo, int id)
    {
        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.NotificacoesAtivas)) return;
        db.Notificacoes.Add(new Notificacao { UsuarioId = usuarioId, Titulo = titulo, Mensagem = mensagem, Tipo = tipo, ReferenciaId = id, Link = "/solicitacoes" });
    }
}

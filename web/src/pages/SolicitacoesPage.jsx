import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Badge, Button, Card, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { useAuth } from '../auth/AuthContext.jsx';
import { api } from '../services/api.js';
import './solicitacoes.css';

const colunas = [[1, 'Solicitações'], [2, 'Em negociação'], [3, 'Agendados'], [4, 'Em execução'], [5, 'Concluídos'], [6, 'Cancelados']];
const etapas = Object.fromEntries(colunas);
const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export default function SolicitacoesPage() {
  const { user } = useAuth();
  const clienteId = user?.id ?? user?.usuarioId;
  const cache = useQueryClient();
  const query = useQuery({ queryKey: ['minhas-solicitacoes'], queryFn: () => api.minhasSolicitacoes() });
  const [forms, setForms] = useState({});
  const mutation = useMutation({
    mutationFn: ({ action, item, quote, form }) => {
      if (action === 'propor') return api.proporOrcamento(item.id, { valor: Number(form.valor), prazoEmDias: form.prazo ? Number(form.prazo) : null, dataPrevisao: form.data ? new Date(form.data).toISOString() : null, oQueEstaIncluso: form.incluso || 'Serviço conforme combinado', observacoes: form.observacoes || null, versao: item.versao });
      if (action === 'aceitar' || action === 'recusar') return api.responderOrcamento(item.id, quote.id, action === 'aceitar', item.versao);
      if (action === 'inicio') return api.moverSolicitacao(item.id, 4, item.versao);
      if (action === 'cancelar') return api.moverSolicitacao(item.id, 6, item.versao, form.motivo);
      if (action === 'concluir') return api.solicitarConclusao(item.id, item.versao);
      return api.confirmarConclusao(item.id, item.versao);
    },
    onSuccess: () => { setForms({}); cache.invalidateQueries({ queryKey: ['minhas-solicitacoes'] }); },
  });
  if (query.isPending) return <div className="page-content"><Skeleton lines={6} /></div>;
  if (query.isError) return <div className="page-content"><ErrorState title="Não foi possível carregar as solicitações" onRetry={query.refetch} /></div>;
  const items = query.data || [];
  return <div className="page-content">
    <PageHeader title="Solicitações" description="Acompanhe pedidos, negocie valores e organize os serviços." />
    {mutation.isError && <p role="alert" className="solicitacoes-erro">{mutation.error.message}</p>}
    {items.length === 0 && <EmptyState title="Nenhuma solicitação ainda" description="Quando você solicitar um serviço ou receber um pedido, ele aparecerá aqui." />}
    <div className="kanban-board" aria-label="Quadro de solicitações">
      {colunas.map(([stage, title]) => <section className="kanban-column" key={stage} aria-label={title}>
        <header><h2>{title}</h2><Badge>{items.filter(item => item.etapa === stage).length}</Badge></header>
        {items.filter(item => item.etapa === stage).map(item => {
          const ehPrestador = clienteId != null && String(item.prestadorId) === String(clienteId);
          const outro = ehPrestador ? item.clienteNome : item.prestadorNome;
          const quote = item.orcamentos?.find(q => q.status === 1 && String(q.autorId) !== String(clienteId));
          const form = forms[item.id] || {};
          const set = (field, value) => setForms(current => ({ ...current, [item.id]: { ...current[item.id], [field]: value } }));
          return <Card className="kanban-card" key={item.id}>
            <h3>{item.ofertaTitulo}</h3><p>{outro}</p><p className="kanban-card__description">{item.descricao}</p>
            <Link to={`/mensagens?id=${item.id}`}>Abrir conversa</Link>
            {item.valor != null && <strong>{money.format(item.valor)}</strong>}
            {item.dataAgendada && <p>Agendado: {new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.dataAgendada))}</p>}
            <details><summary>Orçamentos e histórico</summary>
              {item.orcamentos?.map(q => <p key={q.id}>{money.format(q.valor)} · {q.status === 1 ? 'Pendente' : q.status === 2 ? 'Aceito' : 'Recusado'}{q.dataPrevisao ? ` · ${new Date(q.dataPrevisao).toLocaleDateString('pt-BR')}` : ''}<br />{q.oQueEstaIncluso}</p>)}
              {item.historicos?.map((h, index) => <small key={`${h.ocorreuEm}-${index}`}>{etapas[h.etapaAnterior] || h.etapaAnterior} → {etapas[h.etapaNova] || h.etapaNova} · {new Date(h.ocorreuEm).toLocaleString('pt-BR')}<br /></small>)}
            </details>
            {[1, 2].includes(stage) && <form className="kanban-proposta" onSubmit={event => { event.preventDefault(); mutation.mutate({ action: 'propor', item, form }); }}>
              <label>Propor ou contrapropor valor (R$)<input required min="0.01" step="0.01" type="number" value={form.valor || ''} onChange={e => set('valor', e.target.value)} /></label>
              <label>Data proposta<input required type="datetime-local" value={form.data || ''} onChange={e => set('data', e.target.value)} /></label>
              <label>Prazo em dias<input min="1" type="number" value={form.prazo || ''} onChange={e => set('prazo', e.target.value)} /></label>
              <label>O que está incluso<input required maxLength="1000" value={form.incluso || ''} onChange={e => set('incluso', e.target.value)} /></label>
              <Button loading={mutation.isPending}>Enviar proposta</Button>
            </form>}
            {quote && <div className="kanban-acoes"><Button disabled={mutation.isPending} onClick={() => mutation.mutate({ action: 'aceitar', item, quote })}>Aceitar orçamento</Button><Button variant="secondary" disabled={mutation.isPending} onClick={() => mutation.mutate({ action: 'recusar', item, quote })}>Recusar</Button></div>}
            {stage <= 3 && stage !== 6 && <div className="kanban-acoes"><input aria-label="Motivo do cancelamento" placeholder="Motivo para cancelar" value={form.motivo || ''} onChange={e => set('motivo', e.target.value)} /><Button variant="ghost" disabled={mutation.isPending || !form.motivo?.trim()} onClick={() => mutation.mutate({ action: 'cancelar', item, form })}>Cancelar</Button></div>}
            {stage === 3 && ehPrestador && <Button disabled={mutation.isPending} onClick={() => mutation.mutate({ action: 'inicio', item })}>Iniciar serviço</Button>}
            {stage === 4 && ehPrestador && !item.concluidoPorPrestador && <Button disabled={mutation.isPending} onClick={() => mutation.mutate({ action: 'concluir', item })}>Solicitar conclusão</Button>}
            {stage === 4 && !ehPrestador && item.concluidoPorPrestador && <Button disabled={mutation.isPending} onClick={() => mutation.mutate({ action: 'confirmar', item })}>Confirmar conclusão</Button>}
          </Card>;
        })}
      </section>)}
    </div>
  </div>;
}

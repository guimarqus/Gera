import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, CheckCheck } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button, Card, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './notificacoes.css';

export default function NotificationsPage() {
  const cache = useQueryClient();
  const query = useQuery({ queryKey: ['notificacoes', 1], queryFn: () => api.notificacoes(1, 50), staleTime: 15000 });
  const mutation = useMutation({ mutationFn: ({ id }) => id ? api.marcarNotificacaoLida(id) : api.marcarTodasNotificacoesLidas(), onSuccess: () => { cache.invalidateQueries({ queryKey: ['notificacoes'] }); cache.invalidateQueries({ queryKey: ['notificacoes-nao-lidas'] }); } });
  return <div className="page-content">
    <PageHeader title="Notificações" description="Atualizações sobre seus serviços e conversas." action={<Button variant="secondary" disabled={!query.data?.naoLidas || mutation.isPending} onClick={() => mutation.mutate({})}><CheckCheck size={18} /> Marcar todas como lidas</Button>} />
    {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    {query.isPending && <Skeleton lines={5} />}
    {query.isError && <ErrorState title="Não foi possível carregar as notificações" onRetry={query.refetch} />}
    {query.isSuccess && query.data.itens.length === 0 && <EmptyState icon={Bell} title="Você está em dia" description="Novidades importantes aparecerão aqui." />}
    {query.isSuccess && <ul className="notificacao-lista">{query.data.itens.map(item => <li key={item.id}>
      <Card className={item.lida ? 'notificacao-card notificacao-card--lida' : 'notificacao-card'}>
        <div><strong>{item.titulo}</strong><p>{item.mensagem}</p><time dateTime={item.criadoEm}>{new Date(item.criadoEm).toLocaleString('pt-BR')}</time></div>
        <div className="notificacao-card__acoes">
          {item.link?.startsWith('/') && <Link className="ds-button ds-button--secondary" to={item.link}>Abrir</Link>}
          {!item.lida && <Button variant="ghost" disabled={mutation.isPending} onClick={() => mutation.mutate({ id: item.id })}>Marcar como lida</Button>}
        </div>
      </Card>
    </li>)}</ul>}
    {query.isSuccess && query.data.totalPaginas > 1 && <p>Mostrando as 50 notificações mais recentes.</p>}
  </div>;
}

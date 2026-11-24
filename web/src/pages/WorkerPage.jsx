import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { BriefcaseBusiness } from 'lucide-react';
import { useAuth } from '../auth/AuthContext.jsx';
import { Badge, Button, Card, EmptyState, ErrorState, Field, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './ofertas.css';

const statusLabels = { 1: 'Rascunho', 2: 'Ativa', 3: 'Pausada', 4: 'Em moderação' };
const tamanho = 20;
const formatarData = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' });

export default function WorkerPage() {
  const { token } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [pagina, setPagina] = useState(1);
  const [status, setStatus] = useState('');
  const [confirmarExclusao, setConfirmarExclusao] = useState(null);
  const query = useQuery({
    queryKey: ['minhas-ofertas', token, pagina, status],
    queryFn: () => api.minhasOfertas({ pagina, tamanho, ...(status && { status }) }),
  });
  const mutation = useMutation({
    mutationFn: async ({ acao, oferta }) => {
      if (acao === 'criar') return api.criarOferta();
      if (acao === 'excluir') return api.excluirOferta(oferta.id);
      return api.alterarStatusOferta(oferta.id, oferta.status === 2 ? 3 : 2);
    },
    onSuccess: (resultado, { acao }) => {
      setConfirmarExclusao(null);
      queryClient.invalidateQueries({ queryKey: ['minhas-ofertas', token] });
      if (acao === 'criar') navigate(`/trabalhador/ofertas/nova?rascunho=${resultado.id}`);
    },
  });

  return <div className="page-content">
    <PageHeader title="Minhas ofertas" description="Gerencie seus anúncios e retome seus rascunhos."
      action={<Button loading={mutation.isPending && mutation.variables?.acao === 'criar'}
        disabled={mutation.isPending} onClick={() => mutation.mutate({ acao: 'criar' })}>Nova oferta</Button>} />
    <Field label="Filtrar por status">{attrs => <select {...attrs} className="ds-input" value={status}
      onChange={event => { setStatus(event.target.value); setPagina(1); }}>
      <option value="">Todos</option>
      {Object.entries(statusLabels).map(([valor, texto]) => <option key={valor} value={valor}>{texto}</option>)}
    </select>}</Field>
    {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    {query.isPending && <Skeleton lines={4} />}
    {query.isError && <ErrorState title="Não foi possível carregar suas ofertas" onRetry={query.refetch} />}
    {query.isSuccess && (query.data.length ? <ul className="oferta-lista">
      {query.data.map(oferta => <li key={oferta.id}><Card>
        <Badge tone={oferta.status === 2 ? 'success' : 'neutral'}>{statusLabels[oferta.status]}</Badge>
        <h2>{oferta.titulo || 'Oferta sem título'}</h2>
        <p>{oferta.publicadoEm ? 'Publicada em ' : 'Criada em '}{formatarData.format(new Date(oferta.publicadoEm || oferta.criadoEm))}</p>
        <div className="oferta-acoes">
          {oferta.status !== 4 && <Link className="ds-button ds-button--secondary" to={`/trabalhador/ofertas/${oferta.id}`}>
            {oferta.status === 1 ? 'Continuar rascunho' : 'Editar'}
          </Link>}
          {[2, 3].includes(oferta.status) && <Button variant="secondary" disabled={mutation.isPending}
            onClick={() => mutation.mutate({ acao: 'status', oferta })}>{oferta.status === 2 ? 'Pausar' : 'Reativar'}</Button>}
          <Button variant="ghost" disabled={mutation.isPending} onClick={() => setConfirmarExclusao(oferta.id)}>Excluir</Button>
        </div>
        {confirmarExclusao === oferta.id && <div className="oferta-confirmacao">
          <p>Confirma a exclusão desta oferta?</p>
          <div className="oferta-acoes">
            <Button variant="danger" disabled={mutation.isPending} onClick={() => mutation.mutate({ acao: 'excluir', oferta })}>Confirmar exclusão</Button>
            <Button variant="secondary" disabled={mutation.isPending} onClick={() => setConfirmarExclusao(null)}>Cancelar</Button>
          </div>
        </div>}
      </Card></li>)}
    </ul> : <EmptyState icon={BriefcaseBusiness} title="Nenhuma oferta encontrada"
      description={status ? 'Use outro filtro para ver suas ofertas.' : 'Selecione Nova oferta para criar seu primeiro rascunho.'} />)}
    {(pagina > 1 || query.data?.length === tamanho) && <nav className="oferta-acoes oferta-paginacao" aria-label="Páginas de ofertas">
      <Button variant="secondary" disabled={pagina === 1} onClick={() => setPagina(valor => valor - 1)}>Anterior</Button>
      <span>Página {pagina}</span>
      <Button variant="secondary" disabled={query.data?.length !== tamanho} onClick={() => setPagina(valor => valor + 1)}>Próxima</Button>
    </nav>}
  </div>;
}

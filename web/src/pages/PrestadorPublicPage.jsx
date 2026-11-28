import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, BriefcaseBusiness, Star } from 'lucide-react';
import { Avatar, Badge, Button, Card, EmptyState, ErrorState, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import { useAuth } from '../auth/AuthContext.jsx';
import './public-details.css';

const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const date = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' });
const priceTypes = { 1: 'por hora', 2: 'por diária', 3: 'por m²', 4: 'por serviço', 5: 'a combinar' };

function showPrice(offer) {
  if (offer.tipoPreco === 5 || offer.valor == null) return 'A combinar';
  return `${money.format(offer.valor)} ${priceTypes[offer.tipoPreco] || ''}`;
}

export default function PrestadorPublicPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const valido = /^[1-9]\d{0,9}$/.test(id || '');
  const [params, setParams] = useSearchParams();
  const paginaSolicitada = Number(params.get('pagina'));
  const pagina = Number.isInteger(paginaSolicitada) && paginaSolicitada >= 1 && paginaSolicitada <= 1_000_000
    ? paginaSolicitada : 1;
  const query = useQuery({ queryKey: ['prestador-publico', id, pagina], queryFn: () => api.prestador(id, pagina), enabled: valido });
  const reviews = useQuery({ queryKey: ['avaliacoes-prestador', id], queryFn: () => api.avaliacoesPrestador(id), enabled: valido });
  const cache = useQueryClient();
  const blockList = useQuery({ queryKey: ['bloqueios'], queryFn: api.bloqueios, enabled: Boolean(user) });
  const blocked = (blockList.data || []).some(item => String(item.usuarioId) === String(id));
  const blockAction = useMutation({ mutationFn: () => blocked ? api.desbloquear(id) : api.bloquear(id, null), onSuccess: () => cache.invalidateQueries({ queryKey: ['bloqueios'] }) });
  const report = useMutation({ mutationFn: () => api.denunciar({ tipoAlvo: 2, alvoId: Number(id), motivo: 'Perfil inadequado', descricao: null }) });

  if (!valido) return <div className="page-content public-detail"><EmptyState title="Perfil não encontrado" description="Confira o endereço ou volte para a busca." /></div>;
  if (query.isPending) return <div className="page-content public-detail"><Skeleton lines={6} /></div>;
  if (query.isError) return <div className="page-content public-detail">{query.error.status === 404
    ? <EmptyState title="Perfil não encontrado" description="Este prestador não está disponível." />
    : <ErrorState title="Não foi possível carregar o perfil" onRetry={query.refetch} />}</div>;

  const prestador = query.data;
  return <div className="page-content public-detail">
    <Link className="public-detail__back" to="/"><ArrowLeft size={18} aria-hidden="true" /> Voltar aos serviços</Link>
    <header className="prestador-publico">
      <Avatar name={prestador.nome} src={prestador.fotoUrl} size="lg" />
      <div><p className="page-heading__eyebrow">PERFIL DO PRESTADOR</p><h1>{prestador.nome} {prestador.verificado && <Badge>Verificado</Badge>}</h1>
        <p>{prestador.biografia || 'Profissional cadastrado no Gera.'}</p>
        <div className="prestador-publico__meta">
          <span>Membro desde {date.format(new Date(prestador.desde))}</span>
          <span>{prestador.totalOfertas} {prestador.totalOfertas === 1 ? 'serviço ativo' : 'serviços ativos'}</span>
          {prestador.totalAvaliacoes > 0 && <span><Star size={16} fill="currentColor" aria-hidden="true" />
            {prestador.notaMedia.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} ({prestador.totalAvaliacoes} avaliações)</span>}
        </div>
        {user && String(user.id ?? user.usuarioId) !== String(id) && <div className="kanban-acoes">
          <Button variant="secondary" disabled={blockAction.isPending} onClick={() => blockAction.mutate()}>{blocked ? 'Desbloquear usuário' : 'Bloquear usuário'}</Button>
          <Button variant="ghost" disabled={report.isPending || report.isSuccess} onClick={() => report.mutate()}>Denunciar perfil</Button>
          {blockAction.isError && <p role="alert">{blockAction.error.message}</p>}
          {report.isError && <p role="alert">{report.error.message}</p>}
          {report.isSuccess && <p role="status">Denúncia enviada para análise.</p>}
        </div>}
      </div>
    </header>
    <section aria-labelledby="avaliacoes-titulo" className="public-detail__section">
      <h2 id="avaliacoes-titulo">Avaliações</h2>
      {reviews.isPending && <Skeleton lines={2} />}
      {reviews.isError && <ErrorState title="Não foi possível carregar as avaliações" onRetry={reviews.refetch} />}
      {reviews.data?.length === 0 && <p>Este profissional ainda não recebeu avaliações.</p>}
      <ul>{(reviews.data || []).map(review => <li key={review.id}>
        <p><Star size={15} fill="currentColor" aria-hidden="true" /> {review.nota}/5 · {review.avaliador}</p>
        {review.comentario && <p>{review.comentario}</p>}
        {review.resposta && <p><strong>Resposta do profissional:</strong> {review.resposta}</p>}
      </li>)}</ul>
    </section>
    <header className="prestador-publico__ofertas-heading"><h2>Serviços anunciados</h2><p>Ofertas ativas deste profissional.</p></header>
    {prestador.ofertas.length ? <ul className="prestador-ofertas">
      {prestador.ofertas.map(offer => <li key={offer.id}><Card className="prestador-oferta">
        <Link to={`/ofertas/${offer.id}`} className="prestador-oferta__foto-link">
          {offer.fotoCapaUrl ? <img src={offer.fotoCapaUrl} alt={`Foto do serviço ${offer.titulo}`} loading="lazy" /> :
            <span className="prestador-oferta__sem-foto"><BriefcaseBusiness aria-hidden="true" /></span>}
        </Link>
        <div className="prestador-oferta__corpo"><span>{offer.categoria}</span>
          <h2><Link to={`/ofertas/${offer.id}`}>{offer.titulo}</Link></h2>
          <p>{[offer.bairro, offer.cidade].filter(Boolean).join(' · ')}</p><strong>{showPrice(offer)}</strong>
        </div>
      </Card></li>)}
    </ul> : <EmptyState icon={BriefcaseBusiness} title="Nenhum serviço ativo" description="Este prestador ainda não tem ofertas publicadas." />}
    {prestador.totalPaginas > 1 && <nav className="feed-paginacao" aria-label="Paginação dos serviços do prestador">
      <Button variant="secondary" disabled={pagina <= 1} onClick={() => setParams({ pagina: String(pagina - 1) })}>Anterior</Button>
      <span>Página {pagina} de {prestador.totalPaginas}</span>
      <Button variant="secondary" disabled={pagina >= prestador.totalPaginas} onClick={() => setParams({ pagina: String(pagina + 1) })}>Próxima</Button>
    </nav>}
  </div>;
}

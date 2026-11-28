import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, MapPin, Star } from 'lucide-react';
import { Avatar, Badge, Button, Card, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import { useAuth } from '../auth/AuthContext.jsx';
import './public-details.css';

const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const priceTypes = { 1: 'por hora', 2: 'por diária', 3: 'por m²', 4: 'por serviço', 5: 'a combinar' };

function showPrice(offer) {
  if (offer.tipoPreco === 5 || offer.valor == null) return 'A combinar';
  return `${money.format(offer.valor)} ${priceTypes[offer.tipoPreco] || ''}`;
}

export default function OfertaDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const valido = /^[1-9]\d{0,9}$/.test(id || '');
  const [fotoUrl, setFotoUrl] = useState(null);
  const [descricao, setDescricao] = useState('');
  const queryClient = useQueryClient();
  const solicitacao = useMutation({ mutationFn: () => api.criarSolicitacao(id, descricao), onSuccess: () => { setDescricao(''); queryClient.invalidateQueries({ queryKey: ['minhas-solicitacoes'] }); } });
  const query = useQuery({ queryKey: ['oferta-detalhe', id], queryFn: () => api.ofertaDetalhe(id), enabled: valido });

  if (!valido) return <div className="page-content public-detail"><EmptyState title="Oferta não encontrada" description="Confira o endereço ou volte para a busca." /></div>;
  if (query.isPending) return <div className="page-content public-detail"><Skeleton lines={7} /></div>;
  if (query.isError) return <div className="page-content public-detail">{query.error.status === 404
    ? <EmptyState title="Oferta não encontrada" description="Ela pode ter sido pausada ou removida." />
    : <ErrorState title="Não foi possível carregar a oferta" onRetry={query.refetch} />}</div>;

  const offer = query.data;
  const foto = offer.fotos.find(item => item.url === fotoUrl) || offer.fotos.find(item => item.ehCapa) || offer.fotos[0];
  const atributos = offer.caracteristicas?.atributos || {};
  const local = [offer.bairro, offer.cidade].filter(Boolean).join(' · ');

  return <div className="page-content public-detail">
    <Link className="public-detail__back" to="/"><ArrowLeft size={18} aria-hidden="true" /> Voltar aos serviços</Link>
    <PageHeader eyebrow={offer.categoria} title={offer.titulo} description={local || 'Atendimento em área informada pelo profissional.'} />
    <div className="public-detail__grid">
      <section aria-label="Fotos do serviço">
        {foto && <img className="public-detail__hero" src={foto.url} alt={`Foto de ${offer.titulo}`} />}
        {offer.fotos.length > 1 && <div className="public-detail__galeria">
          {offer.fotos.map((item, index) => <button key={item.url} type="button" aria-pressed={foto?.url === item.url}
            aria-label={`Mostrar foto ${index + 1} de ${offer.fotos.length}`} onClick={() => setFotoUrl(item.url)}>
            <img src={item.urlMiniatura || item.url} alt="" />
          </button>)}
        </div>}
        <Card className="public-detail__section"><h2>Sobre o serviço</h2><p className="public-detail__texto">{offer.descricao}</p>
          {Object.keys(atributos).length > 0 && <><h3>Características</h3><dl className="public-detail__atributos">
            {Object.entries(atributos).map(([chave, valor]) => <div key={chave}><dt>{chave}</dt><dd>{valor}</dd></div>)}
          </dl></>}
          {offer.tags.length > 0 && <div className="public-detail__tags" aria-label="Palavras-chave">{offer.tags.map(tag => <Badge key={tag}>{tag}</Badge>)}</div>}
        </Card>
      </section>
      <aside className="public-detail__aside">
        <Card className="public-detail__section"><span className="public-detail__rotulo">Preço</span><strong className="public-detail__preco">{showPrice(offer)}</strong>
          <div className="public-detail__fatos">
            {local && <span><MapPin size={17} aria-hidden="true" /> {local}</span>}
            <span>Atende em até {offer.raioKm} km</span>
            {offer.disponibilidade && <span><strong>Disponibilidade:</strong> {offer.disponibilidade}</span>}
            {offer.formasPagamento && <span><strong>Formas de pagamento:</strong> {offer.formasPagamento}</span>}
          </div>
          {String(user?.id) !== String(offer.prestadorId) && <form className="pedido-form" onSubmit={event => { event.preventDefault(); solicitacao.mutate(); }}>
            <label htmlFor="solicitacao-descricao">Conte o que precisa ser feito</label>
            <textarea id="solicitacao-descricao" value={descricao} onChange={event => setDescricao(event.target.value)} minLength={10} maxLength={2000} required rows={4} />
            {solicitacao.isError && <p role="alert">{solicitacao.error.message}</p>}
            {solicitacao.isSuccess && <p role="status">Solicitação enviada. Acompanhe em Solicitações.</p>}
            <Button type="submit" loading={solicitacao.isPending}>Solicitar orçamento</Button>
          </form>}
        </Card>
        <Card className="public-detail__prestador">
          <Link to={`/prestadores/${offer.prestadorId}`} aria-label={`Ver perfil de ${offer.prestador}`}>
            <Avatar name={offer.prestador} src={offer.prestadorFotoUrl} size="lg" />
          </Link>
          <div><span className="public-detail__rotulo">Prestador</span>
            <h2><Link to={`/prestadores/${offer.prestadorId}`}>{offer.prestador}</Link></h2>
            <p>{offer.prestadorBiografia || 'Profissional cadastrado no Gera.'}</p>
            <Link to={`/prestadores/${offer.prestadorId}`}>Ver perfil e outros serviços</Link>
          </div>
        </Card>
      </aside>
    </div>
  </div>;
}

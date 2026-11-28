import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Heart, MapPin, Search, Star, Wrench } from 'lucide-react';
import { useAuth } from '../auth/AuthContext.jsx';
import { Button, Card, EmptyState, ErrorState, Field, Input, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './feed.css';

const pageSize = 12;
const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const priceTypes = { 1: 'por hora', 2: 'por diária', 3: 'por m²', 4: 'por serviço', 5: 'a combinar' };
const flatten = categories => (categories || []).flatMap(category => [category, ...(category.subcategorias || [])]);

function price(offer) {
  if (!offer.valor || offer.tipoPreco === 5) return 'A combinar';
  return `${money.format(offer.valor)} ${priceTypes[offer.tipoPreco] || ''}`;
}

export default function HomePage() {
  const { token } = useAuth();
  const queryClient = useQueryClient();
  const [busca, setBusca] = useState('');
  const [filtros, setFiltros] = useState({ ordem: 'proximidade', raioKm: '25', cidade: '', categoriaId: '', precoMinimo: '', precoMaximo: '' });
  const [local, setLocal] = useState(null);
  const [erroLocal, setErroLocal] = useState('');
  const [pagina, setPagina] = useState(1);
  const categoriesQuery = useQuery({ queryKey: ['categorias'], queryFn: api.categories, staleTime: 30 * 60 * 1000 });
  const categorias = flatten(categoriesQuery.data?.categorias || categoriesQuery.data);
  const filtrosFeed = {
    busca: busca.trim(), categoriaId: filtros.categoriaId, precoMinimo: filtros.precoMinimo,
    precoMaximo: filtros.precoMaximo, disponibilidade: filtros.disponibilidade,
    cidade: local ? '' : filtros.cidade.trim(),
    latitude: local?.latitude, longitude: local?.longitude, raioKm: filtros.raioKm,
    ordem: filtros.ordem, pagina, tamanho: pageSize,
  };
  const feedQuery = useQuery({
    queryKey: ['feed-ofertas', filtrosFeed],
    queryFn: () => api.feedOfertas(filtrosFeed),
  });
  const favoritoMutation = useMutation({
    mutationFn: ({ id, ehFavorita }) => ehFavorita ? api.desfavoritarOferta(id) : api.favoritarOferta(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['feed-ofertas'] }),
  });

  function usarLocalizacao() {
    setErroLocal('');
    if (!navigator.geolocation) {
      setErroLocal('Seu navegador não oferece localização. Informe uma cidade para buscar.');
      return;
    }
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => { setLocal({ latitude: coords.latitude, longitude: coords.longitude }); setPagina(1); },
      () => setErroLocal('Não conseguimos acessar sua localização. Informe uma cidade para buscar.'),
      { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 },
    );
  }

  function aplicarFiltros(event) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setPagina(1);
    setBusca(String(form.get('busca') || ''));
    setFiltros(atual => ({ ...atual, cidade: local ? '' : String(form.get('cidade') || '').trim(), categoriaId: String(form.get('categoriaId') || ''), precoMinimo: String(form.get('precoMinimo') || ''), precoMaximo: String(form.get('precoMaximo') || ''), disponibilidade: String(form.get('disponibilidade') || '').trim(), ordem: String(form.get('ordem') || 'proximidade'), raioKm: String(form.get('raioKm') || '25') }));
  }

  function escolherCategoria(id) {
    setFiltros(atual => ({ ...atual, categoriaId: String(id) }));
    setPagina(1);
  }

  return <div className="page-content">
    <PageHeader eyebrow="GERA" title="Encontre um serviço" description="Descubra profissionais que atendem perto de você." />
    <Card className="feed-filtros">
      <form onSubmit={aplicarFiltros}>
        <div className="feed-busca">
          <Input label="O que você precisa?" name="busca" placeholder="Ex.: eletricista, pintura, montagem" defaultValue={busca} />
          <Button type="submit"><Search size={18} aria-hidden="true" /> Buscar</Button>
        </div>
        <div className="feed-local">
          <Input label="Cidade ou bairro" name="cidade" placeholder="Ex.: Manaus, Centro" defaultValue={filtros.cidade} disabled={!!local} />
          <Button variant={local ? 'primary' : 'secondary'} onClick={local ? () => { setLocal(null); setPagina(1); } : usarLocalizacao}>
            <MapPin size={18} aria-hidden="true" /> {local ? 'Usando sua localização · Limpar' : 'Usar minha localização'}
          </Button>
        </div>
        {erroLocal && <p className="feed-aviso" role="status">{erroLocal}</p>}
        {local && <p className="feed-aviso" role="status">Mostrando ofertas em até {filtros.raioKm} km. Sua localização exata não aparece nos resultados.</p>}
        <div className="feed-opcoes">
          <Field label="Categoria">{attrs => <select {...attrs} className="ds-input" name="categoriaId" value={filtros.categoriaId} onChange={event => escolherCategoria(event.target.value)}>
            <option value="">Todas as categorias</option>
            {categorias.map(category => <option key={category.id} value={category.id}>{category.nome}</option>)}
          </select>}</Field>
          <Field label="Preço mínimo">{attrs => <input {...attrs} className="ds-input" name="precoMinimo" type="number" min="0" step="0.01" placeholder="R$ 0" defaultValue={filtros.precoMinimo} />}</Field>
          <Field label="Preço máximo">{attrs => <input {...attrs} className="ds-input" name="precoMaximo" type="number" min="0" step="0.01" placeholder="Sem limite" defaultValue={filtros.precoMaximo} />}</Field>
          <Input label="Disponibilidade" name="disponibilidade" placeholder="Ex.: fins de semana" defaultValue={filtros.disponibilidade} />
          <Field label="Ordenar por">{attrs => <select {...attrs} className="ds-input" name="ordem" defaultValue={filtros.ordem}>
            <option value="proximidade">{local ? 'Mais próximos' : 'Mais recentes'}</option><option value="recentes">Mais recentes</option>
            <option value="menor-preco">Menor preço</option><option value="maior-preco">Maior preço</option>
            <option value="melhor-avaliadas">Melhor avaliados</option>
          </select>}</Field>
          <Field label="Distância máxima">{attrs => <select {...attrs} className="ds-input" name="raioKm" defaultValue={filtros.raioKm} disabled={!local}>
            {[5, 10, 25, 50, 100, 200].map(km => <option key={km} value={km}>{km} km</option>)}
          </select>}</Field>
        </div>
        {categorias.length > 0 && <div className="feed-sugestoes" aria-label="Sugestões de categorias">
          <span>Explore:</span>{categorias.slice(0, 6).map(category => <button key={category.id} type="button" onClick={() => escolherCategoria(category.id)}>{category.nome}</button>)}
        </div>}
      </form>
    </Card>

    {favoritoMutation.isError && <p className="feed-aviso" role="alert">{favoritoMutation.error.message}</p>}
    {feedQuery.isPending && <Skeleton lines={6} className="feed-loading" />}
    {feedQuery.isError && <ErrorState title="Não foi possível carregar os serviços" onRetry={feedQuery.refetch} />}
    {feedQuery.isSuccess && <>
      <div className="feed-resultados" aria-live="polite">
        <h2>{feedQuery.data.total} {feedQuery.data.total === 1 ? 'serviço encontrado' : 'serviços encontrados'}</h2>
        {local && <span>Ordenados pela distância</span>}
      </div>
      {feedQuery.data.ofertas.length ? <ul className="feed-lista">
        {feedQuery.data.ofertas.map(offer => <li key={offer.id}>
          <Card className="feed-card">
            <Link className="feed-card__imagem-link" to={`/ofertas/${offer.id}`} aria-label={`Ver detalhes de ${offer.titulo}`}>
            {offer.fotoCapaUrl ? <img className="feed-card__foto" src={offer.fotoCapaUrl} alt={`Foto do serviço ${offer.titulo}`} loading="lazy" /> :
              <div className="feed-card__foto feed-card__foto--vazia" aria-hidden="true"><Wrench size={36} /></div>}
            </Link>
            <div className="feed-card__corpo">
              <div className="feed-card__topo"><span className="feed-card__categoria">{offer.categoria}</span>
                <Button variant="ghost" className="feed-favorito" disabled={favoritoMutation.isPending}
                  aria-label={offer.ehFavorita ? `Remover ${offer.titulo} dos favoritos` : `Salvar ${offer.titulo} nos favoritos`}
                  aria-pressed={offer.ehFavorita} onClick={() => favoritoMutation.mutate({ id: offer.id, ehFavorita: offer.ehFavorita })}>
                  <Heart size={20} fill={offer.ehFavorita ? 'currentColor' : 'none'} aria-hidden="true" />
                </Button>
              </div>
              <h3><Link to={`/ofertas/${offer.id}`}>{offer.titulo}</Link></h3>
              <p className="feed-card__descricao">{offer.descricao}</p>
              <Link className="feed-card__prestador" to={`/prestadores/${offer.prestadorId}`}>
                {offer.prestadorFotoUrl && <img src={offer.prestadorFotoUrl} alt="" />}
                <span>{offer.prestador}</span>
              </Link>
              <div className="feed-card__detalhes">
                <strong>{price(offer)}</strong>
                <span>{[offer.bairro, offer.cidade, offer.distanciaKm != null && `${offer.distanciaKm.toLocaleString('pt-BR')} km`].filter(Boolean).join(' · ')}</span>
                {offer.totalAvaliacoes > 0 && <span className="feed-nota"><Star size={15} fill="currentColor" aria-hidden="true" /> {offer.notaMedia.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} ({offer.totalAvaliacoes})</span>}
              </div>
            </div>
          </Card>
        </li>)}
      </ul> : <EmptyState icon={Search} title="Nenhum serviço encontrado" description="Tente outra categoria, cidade ou faixa de preço." />}
      {feedQuery.data.totalPaginas > 1 && <nav className="feed-paginacao" aria-label="Paginação dos serviços">
        <Button variant="secondary" disabled={pagina <= 1} onClick={() => setPagina(value => value - 1)}>Anterior</Button>
        <span>Página {pagina} de {feedQuery.data.totalPaginas}</span>
        <Button variant="secondary" disabled={pagina >= feedQuery.data.totalPaginas} onClick={() => setPagina(value => value + 1)}>Próxima</Button>
      </nav>}
    </>}
  </div>;
}

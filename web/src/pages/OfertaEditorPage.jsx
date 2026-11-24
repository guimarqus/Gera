import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/AuthContext.jsx';
import { Button, Card, ErrorState, Field, Input, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './ofertas.css';

const tiposPreco = ['Por hora', 'Por diária', 'Por metro quadrado', 'Por empreitada', 'A combinar'];
const camposNumericos = ['categoriaId', 'tipoPreco', 'valor', 'latitude', 'longitude', 'raioKm'];
const etapas = ['Dados do serviço', 'Área de atendimento', 'Preço', 'Disponibilidade', 'Revisão'];
const intervaloAutosave = 800;
const preenchido = valor => valor != null && valor !== '';

function Editor({ oferta, categorias, token }) {
  const queryClient = useQueryClient();
  const [dados, setDados] = useState(oferta);
  const [salvos, setSalvos] = useState(oferta);
  const [etapa, setEtapa] = useState(0);
  const [erroLocalizacao, setErroLocalizacao] = useState('');
  const [buscandoLocalizacao, setBuscandoLocalizacao] = useState(false);
  const tituloEtapa = useRef(null);
  const alterado = dados !== salvos;
  const coordenadasCompletas = preenchido(dados.latitude) === preenchido(dados.longitude);
  const mutation = useMutation({
    mutationFn: versaoEnviada => {
      const payload = { ...versaoEnviada };
      for (const campo of camposNumericos) {
        const valor = versaoEnviada[campo];
        payload[campo] = preenchido(valor) ? Number(valor) : null;
      }
      return api.editarOferta(oferta.id, payload);
    },
    onSuccess: (salvo, enviado) => {
      setDados(atual => atual === enviado ? salvo : { ...atual, versao: salvo.versao });
      setSalvos(salvo);
      queryClient.setQueryData(['oferta-edicao', token, String(oferta.id)], salvo);
      queryClient.invalidateQueries({ queryKey: ['minhas-ofertas', token] });
    },
  });
  const { mutate, isPending, isError } = mutation;

  useEffect(() => {
    if (!alterado || isPending || isError || !coordenadasCompletas) return undefined;
    const timer = window.setTimeout(() => mutate(dados), intervaloAutosave);
    return () => window.clearTimeout(timer);
  }, [alterado, isPending, isError, coordenadasCompletas, mutate, dados]);

  useEffect(() => {
    if (!alterado) return undefined;
    function avisar(event) { event.preventDefault(); event.returnValue = ''; }
    window.addEventListener('beforeunload', avisar);
    return () => window.removeEventListener('beforeunload', avisar);
  }, [alterado]);

  function alterar(campo, valor) {
    if (mutation.isError && mutation.error.status !== 409) mutation.reset();
    setDados(atual => ({ ...atual, [campo]: valor, ...(campo === 'tipoPreco' && valor === '5' && { valor: '' }) }));
  }

  function irPara(indice) {
    setEtapa(indice);
    tituloEtapa.current?.focus();
  }

  function usarLocalizacao() {
    setErroLocalizacao('');
    if (!navigator.geolocation) {
      setErroLocalizacao('A localização não está disponível. Preencha os dados manualmente.');
      return;
    }
    setBuscandoLocalizacao(true);
    navigator.geolocation.getCurrentPosition(({ coords }) => {
      setDados(atual => ({ ...atual, latitude: coords.latitude, longitude: coords.longitude }));
      setBuscandoLocalizacao(false);
    }, () => {
      setErroLocalizacao('Não foi possível obter sua localização. Você pode informar os dados manualmente.');
      setBuscandoLocalizacao(false);
    }, { timeout: 10000 });
  }

  function entrada(campo, label, props) {
    return <Input label={label} value={dados[campo] ?? ''}
      onChange={event => alterar(campo, event.target.value)} {...props} />;
  }

  return <Card className="oferta-editor">
    <p>Etapa {etapa + 1} de {etapas.length}</p>
    <h2 ref={tituloEtapa} tabIndex={-1}>{etapas[etapa]}</h2>
    <form noValidate onSubmit={event => {
      event.preventDefault();
      if (alterado && !isPending && coordenadasCompletas) mutate(dados);
    }}>
      <fieldset hidden={etapa !== 0}>
        <legend>Dados do serviço</legend>
        <Field label="Categoria">{attrs => <select {...attrs} className="ds-input" value={dados.categoriaId ?? ''}
          onChange={event => alterar('categoriaId', event.target.value)}>
          <option value="">Selecione uma categoria</option>
          {categorias.map(categoria => <optgroup key={categoria.id} label={categoria.nome}>
            <option value={categoria.id}>{categoria.nome}</option>
            {categoria.subcategorias.map(sub => <option key={sub.id} value={sub.id}>{sub.nome}</option>)}
          </optgroup>)}
        </select>}</Field>
        {entrada('titulo', 'Título', { maxLength: 120, placeholder: 'Exemplo: Pintura residencial' })}
        <Field label="Descrição" hint="Conte quais serviços você realiza.">{attrs => <textarea {...attrs}
          className="ds-input oferta-descricao" rows={4} maxLength={2000} value={dados.descricao ?? ''}
          onChange={event => alterar('descricao', event.target.value)} />}</Field>
      </fieldset>
      <fieldset hidden={etapa !== 1}>
        <legend>Área de atendimento</legend>
        <Button variant="secondary" loading={buscandoLocalizacao} onClick={usarLocalizacao}>Usar minha localização</Button>
        {erroLocalizacao && <p role="alert">{erroLocalizacao}</p>}
        <div className="oferta-campos">
          {entrada('cidade', 'Cidade', { maxLength: 120 })}
          {entrada('bairro', 'Bairro', { maxLength: 120 })}
          {entrada('latitude', 'Latitude', { type: 'number', min: -90, max: 90, step: 'any' })}
          {entrada('longitude', 'Longitude', { type: 'number', min: -180, max: 180, step: 'any' })}
          {entrada('raioKm', 'Raio em quilômetros', { type: 'number', min: 1, max: 100 })}
        </div>
      </fieldset>
      <fieldset hidden={etapa !== 2}>
        <legend>Preço</legend>
        <Field label="Tipo de cobrança">{attrs => <select {...attrs} className="ds-input" value={dados.tipoPreco ?? ''}
          onChange={event => alterar('tipoPreco', event.target.value)}>
          <option value="">Selecione</option>
          {tiposPreco.map((label, index) => <option key={label} value={index + 1}>{label}</option>)}
        </select>}</Field>
        {Number(dados.tipoPreco) !== 5 && entrada('valor', 'Valor em reais', { type: 'number', min: 0.01, step: 0.01 })}
      </fieldset>
      <fieldset hidden={etapa !== 3}>
        <legend>Disponibilidade</legend>
        {entrada('disponibilidade', 'Disponibilidade', { maxLength: 200, placeholder: 'Exemplo: Segundas e quartas, das 8h às 17h' })}
        {entrada('formasPagamento', 'Formas de pagamento', { maxLength: 200 })}
      </fieldset>
      {etapa === 4 && <dl className="oferta-revisao">
        <dt>Título</dt><dd>{dados.titulo || 'A preencher'}</dd>
        <dt>Descrição</dt><dd>{dados.descricao || 'A preencher'}</dd>
        <dt>Área de atendimento</dt><dd>{[dados.bairro, dados.cidade].filter(Boolean).join(', ') || 'A preencher'} · {dados.raioKm || 15} km</dd>
        <dt>Preço</dt><dd>{tiposPreco[Number(dados.tipoPreco) - 1] || 'A preencher'}{dados.valor ? ` · R$ ${dados.valor}` : ''}</dd>
        <dt>Disponibilidade</dt><dd>{dados.disponibilidade || 'A preencher'}</dd>
      </dl>}
      {!coordenadasCompletas && <p role="alert">Informe latitude e longitude juntas para salvar.</p>}
      {mutation.isError && <p role="alert">{mutation.error.message}</p>}
      <p role="status">{isPending ? 'Salvando oferta…' : alterado ? 'Há alterações não salvas.' : 'Oferta salva.'}</p>
      <div className="oferta-acoes">
        {etapa > 0 && <Button variant="secondary" onClick={() => irPara(etapa - 1)}>Voltar</Button>}
        {etapa < etapas.length - 1 && <Button onClick={() => irPara(etapa + 1)}>Continuar</Button>}
        <Button variant="secondary" type="submit" loading={isPending}
          disabled={!alterado || isPending || !coordenadasCompletas}>{isError ? 'Tentar salvar novamente' : 'Salvar oferta'}</Button>
        <Link className="ds-button ds-button--secondary" to="/trabalhador" onClick={event => {
          if (mutation.isPending || (alterado && !window.confirm('Sair sem salvar as alterações?'))) event.preventDefault();
        }}>Voltar às ofertas</Link>
      </div>
    </form>
  </Card>;
}

export default function OfertaEditorPage() {
  const { id } = useParams();
  const { token } = useAuth();
  const oferta = useQuery({ queryKey: ['oferta-edicao', token, id], queryFn: () => api.oferta(id) });
  const categorias = useQuery({ queryKey: ['categorias'], queryFn: api.categories });

  return <div className="page-content">
    <PageHeader title={oferta.data?.status === 1 ? 'Editar rascunho' : 'Editar oferta'} />
    {(oferta.isPending || categorias.isPending) && <Skeleton lines={5} />}
    {(oferta.isError || categorias.isError) && <ErrorState onRetry={() => {
      if (oferta.isError) oferta.refetch();
      if (categorias.isError) categorias.refetch();
    }} />}
    {oferta.isSuccess && categorias.isSuccess && (oferta.data.versao
      ? <Editor key={id} oferta={oferta.data} categorias={categorias.data} token={token} />
      : <ErrorState title="Oferta indisponível para edição" description="Você pode editar suas próprias ofertas no painel." />)}
  </div>;
}

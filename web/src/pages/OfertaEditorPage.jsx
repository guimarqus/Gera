import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/AuthContext.jsx';
import { Button, Card, ErrorState, Field, Input, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './ofertas.css';

const tiposPreco = ['Por hora', 'Por diária', 'Por metro quadrado', 'Por empreitada', 'A combinar'];
const camposNumericos = ['categoriaId', 'tipoPreco', 'valor', 'latitude', 'longitude', 'raioKm'];

function Editor({ oferta, categorias, token }) {
  const queryClient = useQueryClient();
  const [dados, setDados] = useState(oferta);
  const [salvos, setSalvos] = useState(oferta);
  const alterado = dados !== salvos;
  const mutation = useMutation({
    mutationFn: () => {
      const payload = { ...dados };
      for (const campo of camposNumericos) {
        const valor = dados[campo];
        payload[campo] = valor == null || valor === '' ? null : Number(valor);
      }
      return api.editarOferta(oferta.id, payload);
    },
    onSuccess: salvo => {
      setDados(salvo);
      setSalvos(salvo);
      queryClient.setQueryData(['oferta-edicao', token, String(oferta.id)], salvo);
      queryClient.invalidateQueries({ queryKey: ['minhas-ofertas', token] });
    },
  });

  useEffect(() => {
    if (!alterado) return undefined;
    function avisar(event) { event.preventDefault(); event.returnValue = ''; }
    window.addEventListener('beforeunload', avisar);
    return () => window.removeEventListener('beforeunload', avisar);
  }, [alterado]);

  function alterar(campo, valor) {
    setDados(atual => ({ ...atual, [campo]: valor, ...(campo === 'tipoPreco' && valor === '5' && { valor: '' }) }));
  }

  function entrada(campo, label, props) {
    return <Input label={label} value={dados[campo] ?? ''}
      onChange={event => alterar(campo, event.target.value)} {...props} />;
  }

  return <Card className="oferta-editor">
    <form onSubmit={event => { event.preventDefault(); if (alterado && !mutation.isPending) mutation.mutate(); }}>
      <fieldset disabled={mutation.isPending}>
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
      <fieldset disabled={mutation.isPending}>
        <legend>Área de atendimento</legend>
        <div className="oferta-campos">
          {entrada('cidade', 'Cidade', { maxLength: 120 })}
          {entrada('bairro', 'Bairro', { maxLength: 120 })}
          {entrada('latitude', 'Latitude', { type: 'number', min: -90, max: 90, step: 'any' })}
          {entrada('longitude', 'Longitude', { type: 'number', min: -180, max: 180, step: 'any' })}
          {entrada('raioKm', 'Raio em quilômetros', { type: 'number', min: 1, max: 100 })}
        </div>
      </fieldset>
      <fieldset disabled={mutation.isPending}>
        <legend>Preço e disponibilidade</legend>
        <Field label="Tipo de cobrança">{attrs => <select {...attrs} className="ds-input" value={dados.tipoPreco ?? ''}
          onChange={event => alterar('tipoPreco', event.target.value)}>
          <option value="">Selecione</option>
          {tiposPreco.map((label, index) => <option key={label} value={index + 1}>{label}</option>)}
        </select>}</Field>
        {Number(dados.tipoPreco) !== 5 && entrada('valor', 'Valor em reais', { type: 'number', min: 0.01, step: 0.01 })}
        {entrada('disponibilidade', 'Disponibilidade', { maxLength: 200, placeholder: 'Exemplo: Segundas e quartas, das 8h às 17h' })}
        {entrada('formasPagamento', 'Formas de pagamento', { maxLength: 200 })}
      </fieldset>
      {mutation.isError && <p role="alert">{mutation.error.message}</p>}
      {mutation.isSuccess && !alterado && <p role="status">Oferta salva.</p>}
      <div className="oferta-acoes">
        <Button type="submit" loading={mutation.isPending} disabled={!alterado || mutation.isPending}>Salvar oferta</Button>
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

import { Link, useSearchParams } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/AuthContext.jsx';
import { Button, PageHeader } from '../design/components.jsx';
import { api } from '../services/api.js';
import OfertaEditorPage from './OfertaEditorPage.jsx';

export default function NovaOfertaPage() {
  const [parametros, setParametros] = useSearchParams();
  const { token } = useAuth();
  const queryClient = useQueryClient();
  const criacao = useMutation({
    mutationFn: api.criarOferta,
    onSuccess: oferta => {
      queryClient.setQueryData(['oferta-edicao', token, String(oferta.id)], oferta);
      queryClient.invalidateQueries({ queryKey: ['minhas-ofertas', token] });
      setParametros({ rascunho: String(oferta.id) }, { replace: true });
    },
  });

  const id = parametros.get('rascunho');
  if (id && /^[1-9]\d{0,9}$/.test(id)) return <OfertaEditorPage rascunhoId={id} />;

  return <div className="page-content">
    <PageHeader title="Nova oferta" description="Inicie um rascunho para descrever seu serviço." />
    {criacao.isError && <p role="alert">{criacao.error.message}</p>}
    <div className="oferta-acoes">
      <Button loading={criacao.isPending} onClick={() => criacao.mutate()}>Iniciar rascunho</Button>
      <Link className="ds-button ds-button--secondary" to="/trabalhador">Voltar às ofertas</Link>
    </div>
  </div>;
}

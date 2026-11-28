import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Settings } from 'lucide-react';
import { Card, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';

export default function SettingsPage() {
  const cache = useQueryClient();
  const query = useQuery({ queryKey: ['preferencias-notificacao'], queryFn: api.preferenciasNotificacao });
  const mutation = useMutation({ mutationFn: api.alterarPreferenciasNotificacao, onSuccess: data => cache.setQueryData(['preferencias-notificacao'], data) });
  return <div className="page-content">
    <PageHeader title="Configurações" description="Preferências da sua conta e experiência." />
    <Card><h2><Settings size={20} aria-hidden="true" /> Notificações</h2>
      {query.isPending && <Skeleton lines={2} />}
      {query.isError && <ErrorState title="Não foi possível carregar as preferências" onRetry={query.refetch} />}
      {query.isSuccess && <label className="preferencia-notificacao"><input type="checkbox" checked={query.data.ativas} disabled={mutation.isPending}
        onChange={event => mutation.mutate(event.target.checked)} /> Receber notificações no Gera</label>}
      {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    </Card>
  </div>;
}

import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Badge, Button, Card, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
import { api } from '../services/api.js';
import './admin.css';

const statusLabel = { 1: 'Pendente', 2: 'Em análise', 3: 'Aceita', 4: 'Rejeitada' };

export default function AdminPage() {
  const cache = useQueryClient();
  const [pareceres, setPareceres] = useState({});
  const [busca, setBusca] = useState('');
  const [categoria, setCategoria] = useState({ nome: '', descricao: '', icone: '', ordem: 0, categoriaPaiId: null, ativo: true });
  const resumo = useQuery({ queryKey: ['admin-resumo'], queryFn: api.adminResumo });
  const reports = useQuery({ queryKey: ['admin-denuncias'], queryFn: () => api.adminDenuncias(1) });
  const categories = useQuery({ queryKey: ['admin-categorias'], queryFn: api.adminCategorias });
  const users = useQuery({ queryKey: ['admin-usuarios', busca], queryFn: () => api.adminUsuarios(busca) });
  const audit = useQuery({ queryKey: ['admin-auditorias'], queryFn: api.adminAuditorias });
  const invalidate = () => ['admin-resumo', 'admin-denuncias', 'admin-categorias', 'admin-usuarios', 'admin-auditorias'].forEach(key => cache.invalidateQueries({ queryKey: [key] }));
  const reportAction = useMutation({ mutationFn: ({ id, status, parecer }) => api.adminDecidirDenuncia(id, { status, parecer }), onSuccess: invalidate });
  const offerAction = useMutation({ mutationFn: ({ id, ativa, motivo }) => api.adminModerarOferta(id, { ativa, motivo }), onSuccess: invalidate });
  const categoryAction = useMutation({ mutationFn: ({ id, data }) => api.adminEditarCategoria(id, data), onSuccess: invalidate });
  const createCategory = useMutation({ mutationFn: () => api.adminCriarCategoria({ ...categoria, categoriaPaiId: categoria.categoriaPaiId ? Number(categoria.categoriaPaiId) : null, ordem: Number(categoria.ordem) }), onSuccess: () => { setCategoria({ nome: '', descricao: '', icone: '', ordem: 0, categoriaPaiId: null, ativo: true }); invalidate(); } });
  const userAction = useMutation({ mutationFn: ({ user, motivo }) => api.adminAlterarUsuario(user.id, { ativo: !user.ativo, motivo }), onSuccess: invalidate });
  const verificationAction = useMutation({ mutationFn: ({ user, motivo }) => api.adminVerificarUsuario(user.id, { verificado: !user.verificado, motivo }), onSuccess: invalidate });
  const failure = [resumo, reports, categories, users, audit].find(query => query.isError);
  if (failure) return <div className="page-content"><ErrorState title={failure.error.status === 403 ? 'Acesso restrito à administração' : 'Não foi possível carregar o painel'} onRetry={() => [resumo, reports, categories, users, audit].forEach(query => query.refetch())} /></div>;
  if ([resumo, reports, categories, users, audit].some(query => query.isPending)) return <div className="page-content"><Skeleton lines={8} /></div>;

  return <div className="page-content admin-page">
    <PageHeader eyebrow="OPERAÇÃO DA PLATAFORMA" title="Administração" description="Modere denúncias, gerencie categorias e acompanhe a atividade do Gera." />
    {reportAction.isError && <p role="alert">{reportAction.error.message}</p>}
    {offerAction.isError && <p role="alert">{offerAction.error.message}</p>}
    {categoryAction.isError && <p role="alert">{categoryAction.error.message}</p>}
    {createCategory.isError && <p role="alert">{createCategory.error.message}</p>}
    {userAction.isError && <p role="alert">{userAction.error.message}</p>}
    {verificationAction.isError && <p role="alert">{verificationAction.error.message}</p>}
    <section className="admin-stats" aria-label="Indicadores">
      {Object.entries({ 'Usuários ativos': resumo.data.usuariosAtivos, 'Ofertas ativas': resumo.data.ofertasAtivas, 'Denúncias pendentes': resumo.data.denunciasPendentes, 'Solicitações abertas': resumo.data.solicitacoesAbertas }).map(([label, value]) => <Card key={label}><span>{label}</span><strong>{value}</strong></Card>)}
    </section>

    <section className="admin-section"><header><h2>Fila de denúncias</h2><p>Denúncias pendentes ordenadas pela data de recebimento.</p></header>
      {!reports.data.length && <EmptyState title="Fila limpa" description="Não há denúncias pendentes para analisar." />}
      <div className="admin-list">{reports.data.map(item => {
        const form = pareceres[item.id] || { status: 2, parecer: '' };
        const change = (key, value) => setPareceres(current => ({ ...current, [item.id]: { ...form, [key]: value } }));
        return <Card key={item.id} className="admin-report"><div className="admin-report__head"><h3>Denúncia #{item.id}</h3><Badge>{statusLabel[item.status]}</Badge></div>
          <p><strong>Alvo:</strong> {item.alvoResumo} · {item.tipoAlvo === 1 ? 'Oferta' : item.tipoAlvo === 2 ? 'Perfil' : 'Mensagem'}</p>
          <p><strong>Motivo:</strong> {item.motivo}</p>{item.descricao && <p>{item.descricao}</p>}
          <small>Denunciado por {item.denunciante} em {new Date(item.criadoEm).toLocaleString('pt-BR')}</small>
          <form onSubmit={event => { event.preventDefault(); reportAction.mutate({ id: item.id, status: Number(form.status), parecer: form.parecer }); }}>
            <label>Parecer<textarea required minLength={3} maxLength={1000} value={form.parecer} onChange={event => change('parecer', event.target.value)} /></label>
            <label>Decisão<select value={form.status} onChange={event => change('status', event.target.value)}><option value="2">Encaminhar para análise</option><option value="3">Aceitar denúncia</option><option value="4">Rejeitar denúncia</option></select></label>
            <Button type="submit" loading={reportAction.isPending}>Registrar decisão</Button>
            {item.tipoAlvo === 1 && !item.ofertaEmModeracao && <Button type="button" variant="secondary" disabled={!form.parecer.trim() || offerAction.isPending} onClick={() => { if (window.confirm('Retirar esta oferta do feed?')) offerAction.mutate({ id: item.alvoId, ativa: false, motivo: form.parecer }); }}>Retirar oferta do ar</Button>}
            {item.tipoAlvo === 1 && item.ofertaEmModeracao && <Button type="button" variant="secondary" disabled={!form.parecer.trim() || offerAction.isPending} onClick={() => { if (window.confirm('Reativar esta oferta no feed?')) offerAction.mutate({ id: item.alvoId, ativa: true, motivo: form.parecer }); }}>Reativar oferta</Button>}
          </form>
        </Card>;
      })}</div>
    </section>

    <section className="admin-section"><header><h2>Categorias</h2><p>Desative categorias para removê-las do cadastro sem apagar histórico.</p></header>
      <form className="admin-category-create" onSubmit={event => { event.preventDefault(); createCategory.mutate(); }}>
        <label>Nome<input required maxLength={100} value={categoria.nome} onChange={event => setCategoria(value => ({ ...value, nome: event.target.value }))} /></label>
        <label>Descrição<input maxLength={300} value={categoria.descricao} onChange={event => setCategoria(value => ({ ...value, descricao: event.target.value }))} /></label>
        <label>Ícone<input maxLength={50} value={categoria.icone} onChange={event => setCategoria(value => ({ ...value, icone: event.target.value }))} /></label>
        <label>Categoria pai<select value={categoria.categoriaPaiId || ''} onChange={event => setCategoria(value => ({ ...value, categoriaPaiId: event.target.value }))}><option value="">Categoria principal</option>{categories.data.map(item => <option key={item.id} value={item.id}>{item.nome}</option>)}</select></label>
        <Button type="submit" loading={createCategory.isPending}>Criar categoria</Button>
      </form>
      <ul className="admin-category-list">{categories.data.map(item => <li key={item.id}><span>{item.nome}{item.categoriaPaiId ? ` · subcategoria de #${item.categoriaPaiId}` : ''}</span>
        <Button variant="secondary" disabled={categoryAction.isPending} onClick={() => { if (!item.ativo || window.confirm('Desativar esta categoria?')) categoryAction.mutate({ id: item.id, data: { ...item, ativo: !item.ativo } }); }}>{item.ativo ? 'Desativar' : 'Ativar'}</Button></li>)}</ul>
    </section>

    <section className="admin-section"><header><h2>Contas</h2><p>Suspender bloqueia novos acessos e revoga sessões de atualização.</p></header>
      <label>Buscar nome ou e-mail<input type="search" maxLength={100} value={busca} onChange={event => setBusca(event.target.value)} /></label>
      <div className="admin-list">{users.data.map(user => {
        const reason = pareceres[`user-${user.id}`] || '';
        return <Card key={user.id} className="admin-user"><div><strong>{user.nome}</strong><span>{user.email}</span></div>
          <label>Motivo/referência interna (sem dados de documentos)<input minLength={3} maxLength={1000} value={reason} onChange={event => setPareceres(current => ({ ...current, [`user-${user.id}`]: event.target.value }))} /></label>
          <Button variant="secondary" disabled={verificationAction.isPending || reason.trim().length < 3} onClick={() => { if (window.confirm(user.verificado ? `Remover o selo de ${user.nome}?` : `Confirmar que a validação de ${user.nome} foi concluída?`)) verificationAction.mutate({ user, motivo: reason }); }}>{user.verificado ? 'Remover selo verificado' : 'Marcar verificado'}</Button>
          <Button variant={user.ativo ? 'secondary' : 'primary'} disabled={userAction.isPending || reason.trim().length < 3} onClick={() => { if (!user.ativo || window.confirm(`Suspender a conta de ${user.nome}?`)) userAction.mutate({ user, motivo: reason }); }}>{user.ativo ? 'Suspender' : 'Reativar'}</Button>
        </Card>;
      })}</div>
    </section>

    <section className="admin-section"><header><h2>Auditoria</h2><p>Registro das ações administrativas mais recentes.</p></header>
      <ol className="admin-audit">{audit.data.map(entry => <li key={entry.id}><strong>{entry.acao}</strong> · {entry.entidade} #{entry.identificador} · admin {entry.administradorId}<br /><small>{entry.motivo} · {new Date(entry.criadoEm).toLocaleString('pt-BR')}</small></li>)}</ol>
    </section>
  </div>;
}

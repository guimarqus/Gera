import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

let accessToken = null;
export const setAccessToken = token => { accessToken = token; };
const base = import.meta.env.VITE_API_BASE || '/api';
const hubUrl = base.startsWith('http') ? `${new URL(base).origin}/hubs/chat` : '/hubs/chat';
export const criarConexaoChat = () => new HubConnectionBuilder().withUrl(hubUrl, { accessTokenFactory: () => accessToken || '' })
  .withAutomaticReconnect().configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error).build();
class ApiError extends Error { constructor(message, status, data) { super(message); this.status = status; this.data = data; } }
let refreshPromise = null;
function refreshOnce() {
  if (!refreshPromise) {
    refreshPromise = request('/auth/refresh', { method: 'POST' }, false)
      .then(result => { setAccessToken(result.accessToken); return result; })
      .finally(() => { refreshPromise = null; });
  }
  return refreshPromise;
}
async function request(path, options = {}, retry = true) {
  const headers = new Headers(options.headers || {});
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  let response;
  try { response = await fetch(`${base}${path}`, { ...options, headers, credentials: 'include' }); }
  catch { throw new ApiError('Não foi possível conectar ao servidor. Verifique se a API está rodando.', 0); }
  if (response.status === 401 && retry && path !== '/auth/refresh' && path !== '/auth/login') {
    try { await refreshOnce(); return request(path, options, false); } catch {}
  }
  const data = response.ok && options.resposta === 'blob'
    ? await response.blob()
    : response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const message = data?.mensagem || data?.detail || (data?.errors && Object.values(data.errors).flat().join(' ')) || data?.title || data?.erros?.map(e => e.mensagem).join(' ') || (response.status === 401 ? 'Usuário ou senha incorretos.' : 'Confira os campos e tente novamente.');
    throw new ApiError(message, response.status, data);
  }
  return data;
}
const json = body => JSON.stringify(body);
export const api = {
  login: body => request('/auth/login', { method: 'POST', body: json(body) }, false),
  register: body => request('/auth/register', { method: 'POST', body: json(body) }, false),
  refresh: refreshOnce,
  logout: () => request('/auth/logout', { method: 'POST' }, false),
  me: () => request('/auth/eu'),
  profileSummary: () => request('/perfil/resumo'),
  categories: () => request('/categorias'),
  feedOfertas: filtros => request(`/ofertas/feed?${new URLSearchParams(Object.entries(filtros).filter(([, valor]) => valor !== '' && valor != null))}`),
  ofertaDetalhe: id => request(`/ofertas/${id}/detalhes`),
  prestador: (id, pagina = 1, tamanho = 12) => request(`/prestadores/${id}?${new URLSearchParams({ pagina, tamanho })}`),
  avaliacoesPrestador: (id, pagina = 1) => request(`/prestadores/${id}/avaliacoes?${new URLSearchParams({ pagina, tamanho: 10 })}`),
  avaliarSolicitacao: (id, body) => request(`/solicitacoes/${id}/avaliacoes`, { method: 'POST', body: json(body) }),
  minhaAvaliacao: id => request(`/solicitacoes/${id}/avaliacoes/minha`),
  responderAvaliacao: (id, body) => request(`/avaliacoes/${id}/resposta`, { method: 'POST', body: json(body) }),
  denunciar: body => request('/denuncias', { method: 'POST', body: json(body) }),
  bloquear: (id, motivo) => request(`/bloqueios/${id}`, { method: 'PUT', body: json({ motivo }) }),
  desbloquear: id => request(`/bloqueios/${id}`, { method: 'DELETE' }),
  bloqueios: () => request('/bloqueios'),
  favoritarOferta: id => request(`/ofertas/${id}/favorito`, { method: 'PUT' }),
  desfavoritarOferta: id => request(`/ofertas/${id}/favorito`, { method: 'DELETE' }),
  modeloOferta: id => request(`/categorias/${id}/modelo-oferta`),
  minhasOfertas: filtros => request(`/ofertas/minhas?${new URLSearchParams(filtros)}`),
  oferta: id => request(`/ofertas/${id}`),
  criarOferta: () => request('/ofertas', { method: 'POST' }),
  editarOferta: (id, dados) => request(`/ofertas/${id}`, { method: 'PUT', body: json(dados) }),
  fotosOferta: id => request(`/ofertas/${id}/fotos`),
  imagemOferta: (url, signal) => request(url.slice('/api'.length), { resposta: 'blob', signal }),
  adicionarFotoOferta: (id, arquivo, versao) => {
    const body = new FormData();
    body.append('arquivo', arquivo);
    body.append('versao', versao);
    return request(`/ofertas/${id}/fotos`, { method: 'POST', body });
  },
  ordenarFotosOferta: (id, fotoIds, versao) => request(`/ofertas/${id}/fotos/ordem`, { method: 'PATCH', body: json({ fotoIds, versao }) }),
  capaOferta: (id, fotoId, versao) => request(`/ofertas/${id}/fotos/${fotoId}/capa`, { method: 'PATCH', body: json({ versao }) }),
  excluirFotoOferta: (id, fotoId, versao) => request(`/ofertas/${id}/fotos/${fotoId}`, { method: 'DELETE', headers: { 'If-Match': `"${versao}"` } }),
  publicarOferta: id => request(`/ofertas/${id}/publicar`, { method: 'POST' }),
  alterarStatusOferta: (id, status) => request(`/ofertas/${id}/status`, { method: 'PATCH', body: json({ status }) }),
  excluirOferta: id => request(`/ofertas/${id}`, { method: 'DELETE' }),
  minhasSolicitacoes: etapa => request(`/solicitacoes/minhas${etapa ? `?etapa=${etapa}` : ''}`),
  criarSolicitacao: (ofertaId, descricao) => request(`/ofertas/${ofertaId}/solicitacoes`, { method: 'POST', body: json({ descricao }) }),
  proporOrcamento: (id, body) => request(`/solicitacoes/${id}/orcamentos`, { method: 'POST', body: json(body) }),
  responderOrcamento: (id, quoteId, aceitar, versao) => request(`/solicitacoes/${id}/orcamentos/${quoteId}/${aceitar ? 'aceitar' : 'recusar'}`, { method: 'POST', body: json({ versao }) }),
  moverSolicitacao: (id, etapa, versao, motivo) => request(`/solicitacoes/${id}/etapa`, { method: 'PATCH', body: json({ etapa, versao, motivo }) }),
  solicitarConclusao: (id, versao) => request(`/solicitacoes/${id}/solicitar-conclusao`, { method: 'POST', body: json({ versao }) }),
  confirmarConclusao: (id, versao) => request(`/solicitacoes/${id}/confirmar-conclusao`, { method: 'POST', body: json({ versao }) }),
  conversas: () => request('/mensagens/conversas'),
  mensagens: (id, antesDe) => request(`/mensagens/${id}${antesDe ? `?antesDe=${antesDe}` : ''}`),
  enviarMensagem: (id, conteudo) => request(`/mensagens/${id}`, { method: 'POST', body: json({ conteudo }) }),
  enviarMidiaChat: (id, arquivo) => { const body = new FormData(); body.append('arquivo', arquivo); return request(`/mensagens/${id}/midia`, { method: 'POST', body }); },
  midiaChat: url => request(url.slice('/api'.length), { resposta: 'blob' }),
  marcarMensagensLidas: id => request(`/mensagens/${id}/lidas`, { method: 'POST' }),
  notificacoes: (pagina = 1, tamanho = 20) => request(`/notificacoes?${new URLSearchParams({ pagina, tamanho })}`),
  marcarNotificacaoLida: id => request(`/notificacoes/${id}/lida`, { method: 'PATCH' }),
  marcarTodasNotificacoesLidas: () => request('/notificacoes/lidas', { method: 'POST' }),
  preferenciasNotificacao: () => request('/notificacoes/preferencias'),
  alterarPreferenciasNotificacao: ativas => request('/notificacoes/preferencias', { method: 'PUT', body: json({ ativas }) }),
  forgotPassword: body => request('/auth/esqueci-senha', { method: 'POST', body: json(body) }, false),
  resetPassword: body => request('/auth/redefinir-senha', { method: 'POST', body: json(body) }, false),
};

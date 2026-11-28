let accessToken = null;
export const setAccessToken = token => { accessToken = token; };
const base = import.meta.env.VITE_API_BASE || '/api';
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
  forgotPassword: body => request('/auth/esqueci-senha', { method: 'POST', body: json(body) }, false),
  resetPassword: body => request('/auth/redefinir-senha', { method: 'POST', body: json(body) }, false),
};

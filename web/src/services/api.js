let accessToken = null;
export const setAccessToken = token => { accessToken = token; };
const base = import.meta.env.VITE_API_BASE || '/api';
class ApiError extends Error { constructor(message, status, data) { super(message); this.status = status; this.data = data; } }
let refreshing = null;
let startupRefresh = null;
async function request(path, options = {}, retry = true) {
  const headers = new Headers(options.headers || {});
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  let response;
  try { response = await fetch(`${base}${path}`, { ...options, headers, credentials: 'include' }); }
  catch { throw new ApiError('Não foi possível conectar ao servidor. Verifique se a API está rodando.', 0); }
  if (response.status === 401 && retry && path !== '/auth/refresh' && path !== '/auth/login') {
    refreshing ||= request('/auth/refresh', { method: 'POST' }, false).finally(() => { refreshing = null; });
    try { const result = await refreshing; setAccessToken(result.accessToken); return request(path, options, false); } catch {}
  }
  const data = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const message = data?.mensagem || data?.title || data?.erros?.map(e => e.mensagem).join(' ') || (response.status === 401 ? 'Usuário ou senha incorretos.' : 'Confira os campos e tente novamente.');
    throw new ApiError(message, response.status, data);
  }
  return data;
}
const json = body => JSON.stringify(body);
export const api = {
  login: body => request('/auth/login', { method: 'POST', body: json(body) }, false),
  register: body => request('/auth/register', { method: 'POST', body: json(body) }, false),
  refresh: () => {
    startupRefresh ||= request('/auth/refresh', { method: 'POST' }, false).finally(() => { startupRefresh = null; });
    return startupRefresh;
  },
  logout: () => request('/auth/logout', { method: 'POST' }, false),
  me: () => request('/auth/eu'),
  forgotPassword: body => request('/auth/esqueci-senha', { method: 'POST', body: json(body) }, false),
  resetPassword: body => request('/auth/redefinir-senha', { method: 'POST', body: json(body) }, false),
};

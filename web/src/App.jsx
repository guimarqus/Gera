import { useState } from 'react';
import { Link, Navigate, Outlet, Route, Routes, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { useAuth } from './auth/AuthContext.jsx';
import { api } from './services/api.js';

function Shell({ children }) {
  return <main className="page"><Link className="brand" to="/">gera<span>.</span></Link>{children}<p className="footer">Gera · Serviços de confiança perto de você</p></main>;
}
function PasswordField({ label = 'Senha', name = 'senha', autoComplete = 'current-password' }) {
  const [visible, setVisible] = useState(false);
  return <label className="field">{label}<span className="password"><input name={name} type={visible ? 'text' : 'password'} autoComplete={autoComplete} required /><button type="button" className="reveal" onClick={() => setVisible(v => !v)} aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}>{visible ? 'Ocultar' : 'Mostrar'}</button></span></label>;
}
function FormError({ error }) {
  if (!error) return null;
  const message = error.message || 'Não foi possível concluir. Tente novamente.';
  const labels = { Nome: 'Nome', Email: 'E-mail', NomeUsuario: 'Nome de usuário', Telefone: 'Telefone', Senha: 'Senha', AceitouTermos: 'Termos de Uso', AceitouPrivacidade: 'Política de Privacidade' };
  const serverErrors = error.data?.errors
    ? Object.entries(error.data.errors).flatMap(([field, messages]) => (messages || []).map(text => `${labels[field] || field}: ${text}`))
    : (error.data?.erros || []).map(item => `${labels[item.campo] || item.campo}: ${item.mensagem}`);
  return <div className="form-error" role="alert">{serverErrors.length ? <ul>{serverErrors.map((text, index) => <li key={index}>{text}</li>)}</ul> : message}</div>;
}
function Login() {
  const { login, ready, token } = useAuth(); const nav = useNavigate(); const loc = useLocation();
  if (!ready) return <Shell><p className="sub">Verificando sua sessão…</p></Shell>;
  if (token) return <Navigate to="/inicio" replace />;
  const mutation = useMutation({ mutationFn: api.login, onSuccess: login });
  async function submit(e) { e.preventDefault(); const f = new FormData(e.currentTarget); try { await mutation.mutateAsync({ usuarioOuEmail: f.get('usuario'), senha: f.get('senha') }); nav(loc.state?.from || '/inicio', { replace: true }); } catch {} }
  return <Shell><section className="card"><p className="eyebrow">BOM TER VOCÊ POR AQUI</p><h1>Entre na sua conta</h1><p className="sub">Encontre profissionais e acompanhe seus serviços.</p><form onSubmit={submit} noValidate><label className="field">Usuário ou e-mail<input name="usuario" autoComplete="username" required /></label><PasswordField /><div className="field-meta"><Link to="/esqueci-senha">Esqueci minha senha</Link></div><FormError error={mutation.error} /><button className="primary" disabled={mutation.isPending}>{mutation.isPending ? 'Entrando…' : 'Entrar'}</button></form><p className="switch">Ainda não tem conta? <Link to="/cadastro">Criar conta</Link></p></section></Shell>;
}
function Register() {
  const { login } = useAuth(); const nav = useNavigate();
  const mutation = useMutation({ mutationFn: api.register, onSuccess: login });
  async function submit(e) { e.preventDefault(); const f = new FormData(e.currentTarget); const data = Object.fromEntries(f); data.aceitouTermos = f.get('termos') === 'on'; data.aceitouPrivacidade = f.get('privacidade') === 'on'; try { await mutation.mutateAsync(data); nav('/inicio', { replace: true }); } catch {} }
  return <Shell><section className="card"><p className="eyebrow">COMECE PELO GERA</p><h1>Crie sua conta</h1><p className="sub">Leva só um minuto.</p><form onSubmit={submit} noValidate><label className="field">Nome completo<input name="nome" autoComplete="name" required /></label><label className="field">E-mail<input name="email" type="email" autoComplete="email" required /></label><label className="field">Nome de usuário<input name="nomeUsuario" autoComplete="username" minLength="3" required /></label><label className="field">Telefone com DDD<input name="telefone" type="tel" autoComplete="tel" placeholder="(11) 99999-9999" required /></label><PasswordField label="Senha (mínimo 8 caracteres, letra e número)" name="senha" autoComplete="new-password" /><label className="check"><input type="checkbox" name="termos" required /> Li e aceito os <a href="/termos" target="_blank" rel="noreferrer">Termos de Uso</a>.</label><label className="check"><input type="checkbox" name="privacidade" required /> Li e aceito a <a href="/privacidade" target="_blank" rel="noreferrer">Política de Privacidade</a>.</label><FormError error={mutation.error} /><button className="primary" disabled={mutation.isPending}>{mutation.isPending ? 'Criando conta…' : 'Criar conta'}</button></form><p className="switch">Já tem conta? <Link to="/login">Entrar</Link></p></section></Shell>;
}
function ForgotPassword() {
  const mutation = useMutation({ mutationFn: api.forgotPassword }); const [sent, setSent] = useState(false);
  async function submit(e) { e.preventDefault(); const email = new FormData(e.currentTarget).get('email'); try { await mutation.mutateAsync({ email }); setSent(true); } catch {} }
  return <Shell><section className="card"><p className="eyebrow">RECUPERE O ACESSO</p><h1>Esqueceu a senha?</h1><p className="sub">Informe seu e-mail e enviaremos instruções para redefini-la.</p>{sent ? <p className="success" role="status">Se o e-mail estiver cadastrado, você receberá instruções para redefinir sua senha.</p> : <form onSubmit={submit}><label className="field">E-mail<input name="email" type="email" autoComplete="email" required /></label><FormError error={mutation.error} /><button className="primary" disabled={mutation.isPending}>{mutation.isPending ? 'Enviando…' : 'Enviar instruções'}</button></form>}<p className="switch"><Link to="/login">Voltar para entrar</Link></p></section></Shell>;
}
function ResetPassword() {
  const [params] = useSearchParams(); const nav = useNavigate(); const [done, setDone] = useState(false);
  const mutation = useMutation({ mutationFn: api.resetPassword });
  async function submit(e) { e.preventDefault(); const f = new FormData(e.currentTarget); try { await mutation.mutateAsync({ email: params.get('email') || f.get('email'), token: params.get('token') || '', novaSenha: f.get('senha') }); setDone(true); } catch {} }
  return <Shell><section className="card"><p className="eyebrow">NOVA SENHA</p><h1>Redefina sua senha</h1>{done ? <><p className="success">Senha alterada. Você já pode entrar.</p><button className="primary" onClick={() => nav('/login')}>Ir para login</button></> : <form onSubmit={submit}><label className="field">E-mail<input name="email" type="email" defaultValue={params.get('email') || ''} required /></label><PasswordField label="Nova senha" name="senha" autoComplete="new-password" /><FormError error={mutation.error} /><button className="primary" disabled={mutation.isPending}>{mutation.isPending ? 'Salvando…' : 'Redefinir senha'}</button></form>}</section></Shell>;
}
function Protected() { const { ready, token } = useAuth(); const location = useLocation(); if (!ready) return <Shell><p className="sub">Verificando sua sessão…</p></Shell>; return token ? <Outlet /> : <Navigate to="/login" replace state={{ from: location.pathname }} />; }
function Home() { const { user, logout } = useAuth(); return <Shell><section className="card"><p className="eyebrow">ÁREA DO CLIENTE</p><h1>Olá, {user?.nome || 'bem-vindo'}!</h1><p className="sub">Sua sessão está protegida e pronta para continuar.</p><button className="primary" onClick={logout}>Sair da conta</button></section></Shell>; }
function EntryRoute() { const { ready, token } = useAuth(); return ready ? <Navigate to={token ? '/inicio' : '/login'} replace /> : <Shell><p className="sub">Verificando sua sessão…</p></Shell>; }
export default function App() { const { ready, token } = useAuth(); return <Routes><Route path="/" element={<EntryRoute />} /><Route path="/login" element={<Login />} /><Route path="/cadastro" element={<Register />} /><Route path="/esqueci-senha" element={<ForgotPassword />} /><Route path="/redefinir-senha" element={<ResetPassword />} /><Route element={<Protected /> }><Route path="/inicio" element={<Home />} /></Route><Route path="*" element={ready ? <Navigate to={token ? '/inicio' : '/login'} replace /> : <Shell><p className="sub">Verificando sua sessão…</p></Shell>} /></Routes>; }

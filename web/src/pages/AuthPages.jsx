import { useState } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { ArrowLeft, Eye, EyeOff } from 'lucide-react';
import { useAuth } from '../auth/AuthContext.jsx';
import { api } from '../services/api.js';
import { Button, Field, Input } from '../design/components.jsx';

function AuthFrame({ eyebrow, title, description, children, footer }) {
  return <main className="auth-frame"><Link className="app-brand auth-frame__brand" to="/">Gera<span>.</span></Link><section className="auth-card"><p className="page-heading__eyebrow">{eyebrow}</p><h1>{title}</h1>{description && <p className="auth-card__description">{description}</p>}{children}{footer && <p className="auth-card__footer">{footer}</p>}</section><p className="auth-frame__caption">Gente boa, serviço bem feito.</p></main>;
}

function ServerError({ error }) {
  if (!error) return null;
  const labels = { Nome: 'Nome', Email: 'E-mail', NomeUsuario: 'Usuário', Telefone: 'Telefone', Senha: 'Senha', AceitouTermos: 'Termos', AceitouPrivacidade: 'Privacidade' };
  const errors = error.data?.errors ? Object.entries(error.data.errors).flatMap(([key, messages]) => messages.map(message => `${labels[key] || key}: ${message}`)) : (error.data?.erros || []).map(item => `${labels[item.campo] || item.campo}: ${item.mensagem}`);
  return <div className="auth-error" role="alert">{errors.length ? <ul>{errors.map((message, i) => <li key={i}>{message}</li>)}</ul> : error.message}</div>;
}

function PasswordInput({ label = 'Senha', name = 'senha' }) {
  const [visible, setVisible] = useState(false);
  return <Field label={label}>{({ id }) => <span className="password-input"><input className="ds-input" id={id} name={name} type={visible ? 'text' : 'password'} autoComplete={name === 'senha' ? 'current-password' : 'new-password'} required /><button type="button" className="password-input__toggle" aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setVisible(value => !value)}>{visible ? <EyeOff size={18} /> : <Eye size={18} />}</button></span>}</Field>;
}

export function LoginPage() {
  const { login } = useAuth(); const navigate = useNavigate(); const location = useLocation();
  const mutation = useMutation({ mutationFn: api.login });
  async function submit(event) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    try { await login(await mutation.mutateAsync({ usuarioOuEmail: form.get('usuario'), senha: form.get('senha') })); navigate(location.state?.from || '/', { replace: true }); } catch {}
  }
  return <AuthFrame eyebrow="BOM TER VOCÊ POR AQUI" title="Entre na sua conta" description="Acesse seus serviços e profissionais favoritos." footer={<>Ainda não tem conta? <Link to="/cadastrar">Criar conta</Link></>}>
    <form onSubmit={submit} noValidate><Input label="Usuário ou e-mail" name="usuario" autoComplete="username" required /><PasswordInput /><div className="auth-form__meta"><Link to="/esqueci-senha">Esqueci minha senha</Link></div><ServerError error={mutation.error} /><Button type="submit" loading={mutation.isPending} className="auth-form__submit">Entrar</Button></form>
  </AuthFrame>;
}

export function RegisterPage() {
  const { login } = useAuth(); const navigate = useNavigate(); const mutation = useMutation({ mutationFn: api.register });
  async function submit(event) {
    event.preventDefault(); const form = new FormData(event.currentTarget); const data = Object.fromEntries(form);
    data.aceitouTermos = form.get('termos') === 'on'; data.aceitouPrivacidade = form.get('privacidade') === 'on';
    try { await login(await mutation.mutateAsync(data)); navigate('/', { replace: true }); } catch {}
  }
  return <AuthFrame eyebrow="COMECE PELO GERA" title="Crie sua conta" description="Seus dados ajudam a conectar você aos serviços certos." footer={<>Já tem conta? <Link to="/entrar">Entrar</Link></>}>
    <form onSubmit={submit} noValidate>
      <Input label="Nome completo" name="nome" autoComplete="name" required />
      <Input label="E-mail" name="email" type="email" autoComplete="email" required />
      <Input label="Nome de usuário" name="nomeUsuario" autoComplete="username" required />
      <Input label="Telefone com DDD" name="telefone" type="tel" autoComplete="tel" placeholder="(11) 99999-9999" required />
      <PasswordInput label="Senha (mínimo 8 caracteres, letra e número)" />
      <label className="auth-consent"><input type="checkbox" name="termos" required /> <span>Li e aceito os <a href="/termos" target="_blank" rel="noreferrer">Termos de Uso</a>.</span></label>
      <label className="auth-consent"><input type="checkbox" name="privacidade" required /> <span>Li e aceito a <a href="/privacidade" target="_blank" rel="noreferrer">Política de Privacidade</a>.</span></label>
      <ServerError error={mutation.error} /><Button type="submit" loading={mutation.isPending} className="auth-form__submit">Criar conta</Button>
    </form>
  </AuthFrame>;
}

export function ForgotPasswordPage() {
  const mutation = useMutation({ mutationFn: api.forgotPassword }); const [sent, setSent] = useState(false);
  async function submit(event) { event.preventDefault(); const email = new FormData(event.currentTarget).get('email'); try { await mutation.mutateAsync({ email }); setSent(true); } catch {} }
  return <AuthFrame eyebrow="RECUPERE O ACESSO" title="Esqueceu sua senha?" description="Informe o e-mail associado à sua conta para receber instruções." footer={<Link to="/entrar"><ArrowLeft size={16} /> Voltar para entrar</Link>}>
    {sent ? <p className="auth-success" role="status">Se o e-mail estiver cadastrado, você receberá instruções para redefinir sua senha.</p> : <form onSubmit={submit}><Input label="E-mail" name="email" type="email" autoComplete="email" required /><ServerError error={mutation.error} /><Button type="submit" loading={mutation.isPending} className="auth-form__submit">Enviar instruções</Button></form>}
  </AuthFrame>;
}

export function ResetPasswordPage() {
  const [params] = useSearchParams(); const navigate = useNavigate(); const [done, setDone] = useState(false); const mutation = useMutation({ mutationFn: api.resetPassword });
  async function submit(event) { event.preventDefault(); const form = new FormData(event.currentTarget); try { await mutation.mutateAsync({ email: params.get('email') || form.get('email'), token: params.get('token') || '', novaSenha: form.get('senha') }); setDone(true); } catch {} }
  return <AuthFrame eyebrow="NOVA SENHA" title="Redefina sua senha" description="Escolha uma senha nova para continuar.">
    {done ? <><p className="auth-success">Senha alterada. Você já pode entrar.</p><Button onClick={() => navigate('/entrar')}>Ir para entrar</Button></> : <form onSubmit={submit}><Input label="E-mail" name="email" type="email" defaultValue={params.get('email') || ''} required /><PasswordInput label="Nova senha" name="novaSenha" /><ServerError error={mutation.error} /><Button type="submit" loading={mutation.isPending} className="auth-form__submit">Redefinir senha</Button></form>}
  </AuthFrame>;
}

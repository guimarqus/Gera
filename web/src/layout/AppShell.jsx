import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, BriefcaseBusiness, ClipboardList, Grid2X2, House, LogOut, Menu, MessageCircle, Search, Settings, ShieldCheck, Sparkles, UserRound, X } from 'lucide-react';
import { useAuth } from '../auth/AuthContext.jsx';
import { Avatar, Badge, Button } from '../design/components.jsx';
import './layout.css';
import { api, criarConexaoChat } from '../services/api.js';

const navItems = [
  { to: '/', label: 'Início', icon: House, role: 'Cliente', end: true },
  { to: '/categorias', label: 'Categorias', icon: Grid2X2 },
  { to: '/assistente', label: 'Ajuda para encontrar', icon: Sparkles },
  { to: '/trabalhador', label: 'Painel do Trabalhador', icon: BriefcaseBusiness, role: 'Prestador' },
  { to: '/mensagens', label: 'Mensagens', icon: MessageCircle, badge: '0' },
  { to: '/solicitacoes', label: 'Solicitações', icon: ClipboardList },
  { to: '/notificacoes', label: 'Notificações', icon: Bell },
  { to: '/perfil', label: 'Meu Perfil', icon: UserRound },
  { to: '/configuracoes', label: 'Configurações', icon: Settings },
  { to: '/admin', label: 'Administração', icon: ShieldCheck, adminOnly: true },
];
const mobileItems = navItems.filter(item => ['/', '/categorias', '/mensagens', '/perfil'].includes(item.to));

function useIsMobile() {
  const [mobile, setMobile] = useState(() => window.matchMedia('(max-width: 767px)').matches);
  useEffect(() => {
    const media = window.matchMedia('(max-width: 767px)');
    const update = event => setMobile(event.matches);
    setMobile(media.matches);
    media.addEventListener('change', update);
    return () => media.removeEventListener('change', update);
  }, []);
  return mobile;
}

function RoleSwitcher() {
  const { activeRole, chooseRole } = useAuth();
  return <div className="role-switcher" role="group" aria-label="Alternar papel ativo">
    {['Cliente', 'Prestador'].map(role => <button key={role} type="button" aria-pressed={activeRole === role} className={activeRole === role ? 'role-switcher__option is-selected' : 'role-switcher__option'} onClick={() => chooseRole(role)}>{role}</button>)}
  </div>;
}

function NotificationShortcut({ token }) {
  const query = useQuery({ queryKey: ['notificacoes', 1], queryFn: () => api.notificacoes(1, 50), staleTime: 15000, refetchOnWindowFocus: true });
  const cache = useQueryClient();
  useEffect(() => {
    if (!token) return undefined;
    const connection = criarConexaoChat();
    const refresh = () => cache.invalidateQueries({ queryKey: ['notificacoes'] });
    connection.on('NotificacaoRecebida', refresh);
    connection.start().catch(() => {});
    return () => { connection.off('NotificacaoRecebida', refresh); connection.stop(); };
  }, [token, cache]);
  const count = query.data?.naoLidas || 0;
  return <Link to="/notificacoes" className="notification-button" aria-label={`Notificações, ${count} não lidas`}>
    <Bell size={21} /><Badge>{count > 9 ? '9+' : count}</Badge>
  </Link>;
}

function NavItems({ items, activeRole, onNavigate, className = '' }) {
  return <div className={className}>{items.map(({ to, label, icon: Icon, badge, role, end }) => <NavLink key={to} to={to} end={end} onClick={onNavigate} data-role-context={role === activeRole || undefined} className={({ isActive }) => `app-nav-link${isActive ? ' is-active' : ''}`}>
    <Icon size={20} aria-hidden="true" /><span>{label}</span>{badge && <Badge className="app-nav-link__badge">{badge}</Badge>}
  </NavLink>)}</div>;
}

export default function AppShell() {
  const mobile = useIsMobile(); const [drawerOpen, setDrawerOpen] = useState(false);
  const { user, token, activeRole, logout } = useAuth(); const navigate = useNavigate();
  const profile = useQuery({ queryKey: ['profile-summary'], queryFn: api.profileSummary, enabled: Boolean(token), staleTime: 30_000 });
  const isAdmin = profile.data?.papeis?.includes('Administrador') === true;
  const visibleNav = navItems.filter(item => !item.adminOnly || isAdmin);
  const name = user?.nome || 'Minha conta';
  async function signOut() { await logout(); setDrawerOpen(false); navigate('/entrar', { replace: true }); }

  return <div className={`app-shell ${mobile ? 'app-shell--mobile' : 'app-shell--desktop'}`}>
    <a className="skip-link" href="#main-content">Pular para o conteúdo</a>
    <header className="app-header">
      {mobile && <Button variant="ghost" className="app-header__menu" aria-label={drawerOpen ? 'Fechar menu adicional' : 'Abrir menu adicional'} aria-expanded={drawerOpen} aria-controls="mobile-drawer" onClick={() => setDrawerOpen(value => !value)}>{drawerOpen ? <X /> : <Menu />}</Button>}
      <Link className="app-brand" to="/" aria-label="Gera, início">Gera<span>.</span></Link>
      <label className="app-search"><Search size={18} aria-hidden="true" /><input type="search" placeholder="Buscar serviços" aria-label="Buscar serviços" readOnly /></label>
      <div className="app-header__actions">
        {!mobile && <RoleSwitcher />}
        <NotificationShortcut token={token} />
        <details className="profile-menu"><summary aria-label={`Abrir menu da conta ${name}`}><Avatar name={name} src={user?.fotoUrl} size="sm" /></summary><div className="profile-menu__panel"><strong>{name}</strong><RoleSwitcher /><NavLink to="/perfil" onClick={() => setDrawerOpen(false)}>Meu Perfil</NavLink><NavLink to="/configuracoes">Configurações</NavLink><button type="button" onClick={signOut}><LogOut size={17} /> Sair</button></div></details>
      </div>
    </header>

    {!mobile && <aside className="desktop-sidebar"><RoleSwitcher /><nav aria-label="Navegação principal"><NavItems items={visibleNav} activeRole={activeRole} className="desktop-sidebar__links" /></nav><Button variant="ghost" className="sign-out" onClick={signOut}><LogOut size={19} /> Sair</Button></aside>}

    {mobile && <nav className="mobile-bottom-nav" aria-label="Navegação principal no celular"><NavItems items={mobileItems} activeRole={activeRole} className="mobile-bottom-nav__links" /></nav>}
    {mobile && drawerOpen && <><button className="drawer-backdrop" aria-label="Fechar menu" onClick={() => setDrawerOpen(false)} /><aside id="mobile-drawer" className="mobile-drawer"><div className="mobile-drawer__head"><span>Mais opções</span><Button variant="ghost" aria-label="Fechar menu adicional" onClick={() => setDrawerOpen(false)}><X /></Button></div><RoleSwitcher /><nav aria-label="Menu adicional"><NavItems items={visibleNav.filter(item => !mobileItems.includes(item))} activeRole={activeRole} onNavigate={() => setDrawerOpen(false)} className="mobile-drawer__links" /></nav><Button variant="ghost" className="sign-out" onClick={signOut}><LogOut size={19} /> Sair</Button></aside></>}

    <main id="main-content" className="app-main" tabIndex={-1}><Outlet /></main>
  </div>;
}

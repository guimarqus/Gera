import { useAuth } from '../auth/AuthContext.jsx';
import { Avatar, EmptyState, PageHeader } from '../design/components.jsx';
export default function ProfilePage() { const { user } = useAuth(); return <div className="page-content"><PageHeader title="Meu Perfil" description="Confira suas informações de conta." />{user ? <div className="profile-summary"><Avatar name={user.nome} src={user.fotoUrl} /><strong>{user.nome}</strong><span>{user.email}</span></div> : <EmptyState title="Perfil indisponível" description="Não foi possível carregar seu perfil." />}</div>; }

import { Link } from 'react-router-dom';
import { EmptyState } from '../design/components.jsx';
export default function NotFoundPage() { return <main className="not-found"><EmptyState title="Página não encontrada" description="O endereço pode ter mudado ou não existe." action={<Link className="ds-button ds-button--primary" to="/">Ir para o início</Link>} /></main>; }

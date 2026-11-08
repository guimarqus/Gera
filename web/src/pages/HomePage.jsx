import { Home } from 'lucide-react';
import { EmptyState, PageHeader } from '../design/components.jsx';
export default function HomePage() { return <div className="page-content"><PageHeader eyebrow="GERA" title="Início" description="Encontre serviços e acompanhe seus pedidos." /><EmptyState icon={Home} title="Seu próximo serviço começa aqui" description="O feed de serviços e recomendações aparecerá nesta página." /></div>; }

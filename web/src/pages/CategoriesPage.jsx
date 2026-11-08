import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Grid2X2 } from 'lucide-react';
import { api } from '../services/api.js';
import { Card, EmptyState, ErrorState, PageHeader, Skeleton } from '../design/components.jsx';
export default function CategoriesPage() {
 const query = useQuery({ queryKey: ['categorias'], queryFn: api.categories });
 if (query.isPending) return <div className="page-content"><PageHeader title="Categorias" /><Skeleton lines={6} /></div>;
 if (query.isError) return <div className="page-content"><PageHeader title="Categorias" /><ErrorState onRetry={() => query.refetch()} /></div>;
 const categories = query.data?.categorias || query.data || [];
 return <div className="page-content"><PageHeader eyebrow="DESCUBRA" title="Categorias" description="Explore serviços por tipo." />{categories.length ? <div className="category-grid">{categories.map(category => <Card key={category.id} className="category-card"><h2><Link to={`/categorias/${category.id}`}>{category.nome}</Link></h2>{(category.subcategorias || []).map(child => <Link key={child.id} to={`/categorias/${child.id}`}>{child.nome}</Link>)}</Card>)}</div> : <EmptyState icon={Grid2X2} title="Nenhuma categoria disponível" description="As categorias de serviços aparecerão aqui." />}</div>;
}

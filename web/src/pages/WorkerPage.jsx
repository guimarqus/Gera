import { BriefcaseBusiness } from 'lucide-react';
import { EmptyState, PageHeader } from '../design/components.jsx';
export default function WorkerPage() { return <div className="page-content"><PageHeader eyebrow="PRESTADOR" title="Painel do Trabalhador" description="Gerencie sua presença profissional no Gera." /><EmptyState icon={BriefcaseBusiness} title="Seu painel está sendo preparado" description="Aqui você poderá acompanhar oportunidades, serviços e avaliações." /></div>; }

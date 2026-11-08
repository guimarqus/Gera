import { MessageCircle } from 'lucide-react';
import { EmptyState, PageHeader } from '../design/components.jsx';
export default function MessagesPage() { return <div className="page-content"><PageHeader title="Mensagens" description="Converse com clientes e profissionais." /><EmptyState icon={MessageCircle} title="Nenhuma conversa por enquanto" description="Suas conversas aparecerão aqui." /></div>; }

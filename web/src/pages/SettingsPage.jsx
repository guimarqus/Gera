import { Settings } from 'lucide-react';
import { EmptyState, PageHeader } from '../design/components.jsx';
export default function SettingsPage() { return <div className="page-content"><PageHeader title="Configurações" description="Preferências da sua conta e experiência." /><EmptyState icon={Settings} title="Configurações em breve" description="As preferências da sua conta poderão ser ajustadas aqui." /></div>; }

import { lazy, Suspense } from 'react';
import { Route, Routes } from 'react-router-dom';
import AppShell from './layout/AppShell.jsx';
import { Skeleton } from './design/components.jsx';
import ProtectedRoute from './routing/ProtectedRoute.jsx';
import GlobalErrorBoundary from './routing/GlobalErrorBoundary.jsx';

const LoginPage = lazy(() => import('./pages/AuthPages.jsx').then(m => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import('./pages/AuthPages.jsx').then(m => ({ default: m.RegisterPage })));
const ForgotPasswordPage = lazy(() => import('./pages/AuthPages.jsx').then(m => ({ default: m.ForgotPasswordPage })));
const ResetPasswordPage = lazy(() => import('./pages/AuthPages.jsx').then(m => ({ default: m.ResetPasswordPage })));
const HomePage = lazy(() => import('./pages/HomePage.jsx'));
const OfertaDetailPage = lazy(() => import('./pages/OfertaDetailPage.jsx'));
const PrestadorPublicPage = lazy(() => import('./pages/PrestadorPublicPage.jsx'));
const CategoriesPage = lazy(() => import('./pages/CategoriesPage.jsx'));
const CategoryDetailPage = lazy(() => import('./pages/CategoryDetailPage.jsx'));
const WorkerPage = lazy(() => import('./pages/WorkerPage.jsx'));
const SolicitacoesPage = lazy(() => import('./pages/SolicitacoesPage.jsx'));
const OfertaEditorPage = lazy(() => import('./pages/OfertaEditorPage.jsx'));
const NovaOfertaPage = lazy(() => import('./pages/NovaOfertaPage.jsx'));
const MessagesPage = lazy(() => import('./pages/MessagesPage.jsx'));
const ProfilePage = lazy(() => import('./pages/ProfilePage.jsx'));
const SettingsPage = lazy(() => import('./pages/SettingsPage.jsx'));
const NotificationsPage = lazy(() => import('./pages/NotificationsPage.jsx'));
const NotFoundPage = lazy(() => import('./pages/NotFoundPage.jsx'));

function Loading() { return <div className="page-content"><Skeleton lines={5} /></div>; }
export default function App() {
  return <GlobalErrorBoundary><Suspense fallback={<Loading />}><Routes>
    <Route path="/entrar" element={<LoginPage />} />
    <Route path="/cadastrar" element={<RegisterPage />} />
    <Route path="/esqueci-senha" element={<ForgotPasswordPage />} />
    <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
    <Route element={<ProtectedRoute />}><Route element={<AppShell />}>
      <Route path="/" element={<HomePage />} />
      <Route path="/ofertas/:id" element={<OfertaDetailPage />} />
      <Route path="/prestadores/:id" element={<PrestadorPublicPage />} />
      <Route path="/categorias" element={<CategoriesPage />} />
      <Route path="/categorias/:id" element={<CategoryDetailPage />} />
      <Route path="/trabalhador" element={<WorkerPage />} />
      <Route path="/solicitacoes" element={<SolicitacoesPage />} />
      <Route path="/trabalhador/ofertas/nova" element={<NovaOfertaPage />} />
      <Route path="/trabalhador/ofertas/:id" element={<OfertaEditorPage />} />
      <Route path="/mensagens" element={<MessagesPage />} />
      <Route path="/notificacoes" element={<NotificationsPage />} />
      <Route path="/perfil" element={<ProfilePage />} />
      <Route path="/configuracoes" element={<SettingsPage />} />
    </Route></Route>
    <Route path="*" element={<NotFoundPage />} />
  </Routes></Suspense></GlobalErrorBoundary>;
}

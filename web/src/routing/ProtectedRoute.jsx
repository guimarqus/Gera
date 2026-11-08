import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext.jsx';
import { Skeleton } from '../design/components.jsx';

export default function ProtectedRoute() {
  const { ready, token } = useAuth(); const location = useLocation();
  if (!ready) return <main className="route-loading"><Skeleton lines={4} /></main>;
  if (!token) return <Navigate to="/entrar" replace state={{ from: `${location.pathname}${location.search}${location.hash}` }} />;
  return <Outlet />;
}

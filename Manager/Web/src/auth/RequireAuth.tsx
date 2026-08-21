import { Navigate } from 'react-router-dom';
import { getToken, isTokenExpired, getIsSystemAdmin } from './authToken';

interface RequireAuthProps {
  children: React.ReactNode;
  requireAdmin?: boolean;
}

export default function RequireAuth({ children, requireAdmin }: RequireAuthProps) {
  if (!getToken() || isTokenExpired()) {
    return <Navigate to="/login" replace />;
  }

  if (requireAdmin && !getIsSystemAdmin()) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

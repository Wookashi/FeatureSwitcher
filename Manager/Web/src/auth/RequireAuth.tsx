import { Navigate } from 'react-router-dom';
import { getToken, isTokenExpired, getRole } from './authToken';

interface RequireAuthProps {
  children: React.ReactNode;
  requiredRole?: string;
}

export default function RequireAuth({ children, requiredRole }: RequireAuthProps) {
  if (!getToken() || isTokenExpired()) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole) {
    const role = getRole();
    if (role !== requiredRole) {
      return <Navigate to="/" replace />;
    }
  }

  return <>{children}</>;
}

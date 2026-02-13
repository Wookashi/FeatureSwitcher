import { Navigate } from 'react-router-dom';
import { getToken, isTokenExpired } from './authToken';

interface RequireAuthProps {
  children: React.ReactNode;
}

export default function RequireAuth({ children }: RequireAuthProps) {
  if (!getToken() || isTokenExpired()) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

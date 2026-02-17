import { getToken, getRole, isTokenExpired } from './authToken';

export function useAuth() {
  const token = getToken();
  const isAuthenticated = !!token && !isTokenExpired();
  const role = getRole() ?? '';

  return {
    isAuthenticated,
    role,
    isAdmin: role === 'Admin',
    isEditor: role === 'Editor',
    isViewer: role === 'Viewer',
    canToggle: role === 'Admin' || role === 'Editor',
  };
}

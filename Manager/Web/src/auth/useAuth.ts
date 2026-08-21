import { getToken, getIsSystemAdmin, isTokenExpired } from './authToken';

export function useAuth() {
  const token = getToken();
  const isAuthenticated = !!token && !isTokenExpired();
  const isAdmin = getIsSystemAdmin();

  return {
    isAuthenticated,
    isAdmin,
  };
}

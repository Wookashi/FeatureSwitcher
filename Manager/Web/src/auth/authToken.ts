const TOKEN_KEY = 'jwt_token';
const IS_ADMIN_KEY = 'is_system_admin';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function removeToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(IS_ADMIN_KEY);
}

export function isTokenExpired(): boolean {
  const token = getToken();
  if (!token) return true;

  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    // exp is in seconds, Date.now() is in milliseconds
    return payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

export function setIsSystemAdmin(isSystemAdmin: boolean): void {
  localStorage.setItem(IS_ADMIN_KEY, String(isSystemAdmin));
}

export function getIsSystemAdmin(): boolean {
  return localStorage.getItem(IS_ADMIN_KEY) === 'true';
}

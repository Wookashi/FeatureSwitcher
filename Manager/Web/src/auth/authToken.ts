const TOKEN_KEY = 'jwt_token';
const ROLE_KEY = 'user_role';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function removeToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(ROLE_KEY);
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

export function setRole(role: string): void {
  localStorage.setItem(ROLE_KEY, role);
}

export function getRole(): string | null {
  return localStorage.getItem(ROLE_KEY);
}

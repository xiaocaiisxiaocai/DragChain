import { ref } from 'vue';

export interface DataInfo<T = number> {
  accessToken: string;
  refreshToken: string;
  expires: T;
  avatar?: string;
  username?: string;
  nickname?: string;
  roles?: string[];
  permissions?: string[];
}

const tokenKey = 'selection-software-token';

/** Reactive counter — increments on every setToken/removeToken call to trigger Vue computed re-evaluation */
export const authVersion = ref(0);

export function getToken(): DataInfo<number> | null {
  const raw = localStorage.getItem(tokenKey);
  if (!raw) return null;

  try {
    const token = JSON.parse(raw) as DataInfo<number>;
    if (!isSupportedAccessToken(token.accessToken)) {
      removeToken();
      return null;
    }

    return token;
  } catch {
    removeToken();
    return null;
  }
}

export function setToken(data: DataInfo<Date | string | number>) {
  const expires = typeof data.expires === 'number'
    ? data.expires
    : new Date(data.expires).getTime();

  localStorage.setItem(tokenKey, JSON.stringify({
    ...data,
    expires
  }));
  authVersion.value++;
}

export function removeToken() {
  localStorage.removeItem(tokenKey);
  authVersion.value++;
}

export function formatToken(token: string) {
  return `Bearer ${token}`;
}

export function isLoggedIn() {
  const token = getToken();
  return !!token?.accessToken && Number(token.expires) > Date.now();
}

function isSupportedAccessToken(accessToken?: string) {
  return !!accessToken && accessToken.includes('.');
}

export function getPermissions() {
  return getToken()?.permissions ?? [];
}

export function hasPerms(value?: string | string[]) {
  if (!value) return true;
  const permissions = getPermissions();
  if (permissions.includes('*:*:*')) return true;
  return Array.isArray(value)
    ? value.every(item => permissions.includes(item))
    : permissions.includes(value);
}

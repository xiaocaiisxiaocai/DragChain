/**
 * 认证相关 API
 */

import { getToken, setToken, type DataInfo as StoredToken } from '@/utils/auth';

const viteEnv = import.meta.env ?? {};
const defaultApiOrigin = viteEnv.DEV ? 'http://localhost:5256' : '';
const API_ORIGIN = viteEnv.VITE_API_BASE || defaultApiOrigin;

export interface TokenData {
  accessToken: string;
  refreshToken: string;
  expires: string | number;
  avatar: string;
  username: string;
  nickname: string;
  roles: string[];
  permissions: string[];
}

export interface DataInfo<T> {
  data: T;
  message?: string;
}

let refreshPromise: Promise<StoredToken<number>> | null = null;

/**
 * 刷新访问令牌
 * @param refreshToken - 刷新令牌
 * @returns 新的访问令牌数据
 */
export async function refreshAccessToken(refreshToken: string): Promise<DataInfo<TokenData>> {
  const res = await fetch(`${API_ORIGIN}/refresh-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  });

  if (!res.ok) {
    throw new Error(`Token refresh failed: ${res.status}`);
  }

  return res.json();
}

/**
 * 统一刷新并替换本地登录态。两个请求封装共用同一个 Promise，避免并发 401
 * 重复轮换 refreshToken。服务端必须返回当前用户的完整角色与权限元数据。
 */
export function refreshStoredToken(): Promise<StoredToken<number>> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = performStoredTokenRefresh().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

async function performStoredTokenRefresh(): Promise<StoredToken<number>> {
  const currentToken = getToken();
  if (!currentToken?.refreshToken) {
    throw new Error('缺少刷新令牌');
  }

  const response = await refreshAccessToken(currentToken.refreshToken);
  if (!isCompleteTokenData(response.data)) {
    throw new Error('刷新接口未返回完整用户信息');
  }

  // 刷新后的用户、角色和权限均以服务端当前状态为准，不能混入旧登录态。
  setToken(response.data);
  const storedToken = getToken();
  if (!storedToken) {
    throw new Error('刷新后的登录信息无效');
  }
  return storedToken;
}

function isCompleteTokenData(value: unknown): value is TokenData {
  if (!value || typeof value !== 'object') return false;
  const data = value as Partial<TokenData>;
  return typeof data.accessToken === 'string'
    && typeof data.refreshToken === 'string'
    && (typeof data.expires === 'string' || typeof data.expires === 'number')
    && typeof data.avatar === 'string'
    && typeof data.username === 'string'
    && typeof data.nickname === 'string'
    && Array.isArray(data.roles)
    && data.roles.every(role => typeof role === 'string')
    && Array.isArray(data.permissions)
    && data.permissions.every(permission => typeof permission === 'string');
}

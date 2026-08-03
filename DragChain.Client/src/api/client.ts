import { formatToken, getToken } from '@/utils/auth';
import { handleAuthFailure } from '@/utils/authFailure';
import { refreshStoredToken } from './auth';

const viteEnv = import.meta.env ?? {};
const defaultApiBase = viteEnv.DEV ? 'http://localhost:5256' : '';
const API_BASE = `${viteEnv.VITE_API_BASE || defaultApiBase}/api`;

interface ApiRequestOptions extends RequestInit {
  parseJson?: boolean;
  _isRetry?: boolean; // 内部标记，避免无限重试
}

function isAuthenticationEndpoint(path: string) {
  const normalizedPath = path.split('?', 1)[0].toLowerCase().replace(/\/$/, '');
  return normalizedPath === '/auth/login' || normalizedPath === '/auth/refresh-token';
}

async function request<T>(path: string, options?: ApiRequestOptions): Promise<T> {
  const url = `${API_BASE}${path}`;
  const { parseJson = true, _isRetry = false, ...fetchOptions } = options || {};
  const headers = new Headers(fetchOptions.headers);
  if (!headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const token = getToken();
  if (token?.accessToken && Number(token.expires) > Date.now()) {
    headers.set('Authorization', formatToken(token.accessToken));
  }

  const res = await fetch(url, {
    ...fetchOptions,
    headers
  });

  if (!res.ok) {
    if (res.status === 401) {
      if (isAuthenticationEndpoint(path)) {
        throw new Error('认证失败');
      }

      if (_isRetry) {
        handleAuthFailure();
        throw new Error('登录已失效，请重新登录');
      }

      const currentToken = getToken();
      if (currentToken?.refreshToken) {
        try {
          await refreshStoredToken();
          return request<T>(path, { ...options, _isRetry: true });
        } catch {
          handleAuthFailure();
          throw new Error('Token 刷新失败，请重新登录');
        }
      } else {
        handleAuthFailure();
        throw new Error('未登录或登录已过期');
      }
    }
    const err = await res.text();
    throw new Error(`API Error ${res.status}: ${err}`);
  }

  if (res.status === 204 || res.headers.get('content-length') === '0') {
    return undefined as T;
  }

  if (!parseJson) {
    return undefined as T;
  }

  const text = await res.text();
  if (!text) {
    return undefined as T;
  }

  const contentType = res.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
    throw new Error(`接口 ${url} 返回的不是 JSON，请检查后端是否已更新并正常启动。`);
  }

  return JSON.parse(text) as T;
}

export const client = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' })
};

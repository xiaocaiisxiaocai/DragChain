import { formatToken, getToken } from '@/utils/auth';
import { handleAuthFailure } from '@/utils/authFailure';
import { refreshStoredToken } from '@/api/auth';

type RequestMethods = 'get' | 'post' | 'put' | 'delete';

type RequestConfig = {
  params?: Record<string, unknown>;
  data?: unknown;
  headers?: Record<string, string>;
  responseType?: 'blob' | 'json';
  beforeResponseCallback?: (response: any) => unknown;
};

type InternalRequestConfig = RequestConfig & {
  _isRetry?: boolean;
};

const viteEnv = import.meta.env ?? {};
const defaultApiOrigin = viteEnv.DEV ? 'http://localhost:5256' : '';

function toUrl(path: string, params?: Record<string, unknown>) {
  const base = viteEnv.VITE_API_BASE || defaultApiOrigin;
  const url = new URL(path, base || window.location.origin);

  Object.entries(params ?? {}).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    if (Array.isArray(value)) {
      value.forEach(item => url.searchParams.append(key, String(item)));
    } else {
      url.searchParams.set(key, String(value));
    }
  });

  if (!base) return `${url.pathname}${url.search}`;
  return url.toString();
}

function isAuthenticationEndpoint(path: string) {
  const normalizedPath = path.split('?', 1)[0].toLowerCase().replace(/\/$/, '');
  return normalizedPath === '/login'
    || normalizedPath === '/refresh-token'
    || normalizedPath === '/api/auth/login'
    || normalizedPath === '/api/auth/refresh-token';
}

async function request<T>(method: RequestMethods, path: string, config: InternalRequestConfig = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json, text/plain, */*',
    ...config.headers
  };

  const token = getToken();
  if (token?.accessToken && Number(token.expires) > Date.now()) {
    headers.Authorization = formatToken(token.accessToken);
  }

  let body: BodyInit | undefined;
  if (config.data instanceof FormData) {
    body = config.data;
  } else if (config.data !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(config.data);
  }

  const response = await fetch(toUrl(path, config.params), {
    method: method.toUpperCase(),
    headers,
    body
  });

  if (response.status === 401) {
    if (isAuthenticationEndpoint(path)) {
      const message = await response.text();
      throw new Error(message || '认证失败');
    }

    if (config._isRetry) {
      handleAuthFailure();
      throw new Error('登录已失效，请重新登录');
    }

    const currentToken = getToken();
    if (currentToken?.refreshToken) {
      try {
        await refreshStoredToken();
        return request<T>(method, path, { ...config, _isRetry: true });
      } catch {
        handleAuthFailure();
        throw new Error('Token 刷新失败，请重新登录');
      }
    }

    handleAuthFailure();
    throw new Error('未登录或登录已过期');
  }

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `请求失败：${response.status}`);
  }

  if (response.status === 204) return undefined as T;
  if (config.responseType === 'blob') return await response.blob() as T;

  const text = await response.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

export const http = {
  request<T>(method: RequestMethods, path: string, config?: RequestConfig, _extra?: RequestConfig) {
    return request<T>(method, path, config);
  },
  get<T, _P = void>(path: string, config?: RequestConfig, _extra?: RequestConfig) {
    return request<T>('get', path, config);
  },
  post<T, _P = unknown>(path: string, config?: RequestConfig, _extra?: RequestConfig) {
    return request<T>('post', path, config);
  }
};

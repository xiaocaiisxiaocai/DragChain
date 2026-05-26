const defaultApiBase = import.meta.env.DEV ? 'http://localhost:5256' : '';
const API_BASE = `${import.meta.env.VITE_API_BASE || defaultApiBase}/api`;

interface ApiRequestOptions extends RequestInit {
  parseJson?: boolean;
}

async function request<T>(path: string, options?: ApiRequestOptions): Promise<T> {
  const url = `${API_BASE}${path}`;
  const { parseJson = true, ...fetchOptions } = options || {};
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...fetchOptions
  });

  if (!res.ok) {
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

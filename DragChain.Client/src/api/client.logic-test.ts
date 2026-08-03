import { http } from '../utils/http';
import { client } from './client';

const storage = new Map<string, string>();
globalThis.localStorage = {
  getItem: key => storage.get(key) ?? null,
  setItem: (key, value) => storage.set(key, value),
  removeItem: key => storage.delete(key),
  clear: () => storage.clear(),
  key: index => Array.from(storage.keys())[index] ?? null,
  get length() {
    return storage.size;
  }
} as Storage;

Object.defineProperty(globalThis, 'window', {
  configurable: true,
  value: {
    location: {
      origin: 'http://localhost',
      pathname: '/login',
      search: '',
      hash: '',
      assign: () => undefined
    }
  }
});

localStorage.setItem('selection-software-token', JSON.stringify({
  accessToken: 'signed.token-for-client-test',
  refreshToken: 'refresh-token',
  expires: Date.now() + 60_000
}));

let capturedAuthorization = '';
globalThis.fetch = (async (_input: RequestInfo | URL, init?: RequestInit) => {
  const headers = new Headers(init?.headers);
  capturedAuthorization = headers.get('Authorization') ?? '';
  return new Response('[]', {
    status: 200,
    headers: { 'content-type': 'application/json' }
  });
}) as typeof fetch;

await client.get('/trunking');

if (capturedAuthorization !== 'Bearer signed.token-for-client-test') {
  throw new Error('client 请求封装必须带上登录 Authorization');
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' }
  });
}

function setStoredToken(overrides: Record<string, unknown> = {}) {
  localStorage.setItem('selection-software-token', JSON.stringify({
    accessToken: 'signed.old-access',
    refreshToken: 'signed.old-refresh',
    expires: Date.now() + 60_000,
    avatar: 'old-avatar',
    username: 'E001',
    nickname: '旧用户',
    roles: ['user'],
    permissions: ['page:trunking'],
    ...overrides
  }));
}

setStoredToken();
let refreshCalls = 0;
let initialUnauthorizedCalls = 0;
let retriedCalls = 0;
let releaseSuccessfulRefresh!: () => void;
const successfulRefreshGate = new Promise<void>(resolve => {
  releaseSuccessfulRefresh = resolve;
});

globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
  const url = String(input);
  const authorization = new Headers(init?.headers).get('Authorization') ?? '';

  if (url.endsWith('/refresh-token')) {
    refreshCalls++;
    const body = JSON.parse(String(init?.body)) as { refreshToken?: string };
    if (body.refreshToken !== 'signed.old-refresh') {
      throw new Error('刷新请求必须提交当前 refreshToken');
    }
    await successfulRefreshGate;
    return jsonResponse({
      success: true,
      data: {
        accessToken: 'signed.new-access',
        refreshToken: 'signed.new-refresh',
        expires: '2030-01-02T03:04:05Z',
        avatar: 'new-avatar',
        username: 'E002',
        nickname: '新用户',
        roles: ['admin'],
        permissions: ['page:sensor', 'api:sensor:write']
      }
    });
  }

  if (authorization === 'Bearer signed.old-access') {
    initialUnauthorizedCalls++;
    if (initialUnauthorizedCalls === 2) releaseSuccessfulRefresh();
    return jsonResponse({ message: 'expired' }, 401);
  }

  if (authorization === 'Bearer signed.new-access') {
    retriedCalls++;
    return jsonResponse({ ok: true });
  }

  return jsonResponse({ message: 'unexpected token' }, 401);
}) as typeof fetch;

await Promise.all([
  client.get('/from-client'),
  http.get('/api/from-http')
]);

if (refreshCalls !== 1) {
  throw new Error('两个请求封装的并发 401 必须共用一次 /refresh-token 请求');
}
if (retriedCalls !== 2) {
  throw new Error('刷新成功后两个请求封装都必须使用新 accessToken 重试');
}

const refreshedToken = JSON.parse(localStorage.getItem('selection-software-token') ?? '{}') as {
  expires?: number;
  avatar?: string;
  username?: string;
  nickname?: string;
  roles?: string[];
  permissions?: string[];
};
if (refreshedToken.expires !== Date.parse('2030-01-02T03:04:05Z')) {
  throw new Error('刷新响应的 expires 字段必须转换为本地时间戳');
}
if (refreshedToken.username !== 'E002'
  || refreshedToken.nickname !== '新用户'
  || refreshedToken.avatar !== 'new-avatar'
  || refreshedToken.roles?.join(',') !== 'admin'
  || refreshedToken.permissions?.join(',') !== 'page:sensor,api:sensor:write') {
  throw new Error('刷新后必须用服务端完整用户元数据替换旧角色与权限');
}

setStoredToken();
let failedRefreshCalls = 0;
let failedUnauthorizedCalls = 0;
let releaseFailedRefresh!: () => void;
const failedRefreshGate = new Promise<void>(resolve => {
  releaseFailedRefresh = resolve;
});

globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
  const url = String(input);
  const authorization = new Headers(init?.headers).get('Authorization') ?? '';

  if (url.endsWith('/refresh-token')) {
    failedRefreshCalls++;
    await failedRefreshGate;
    return jsonResponse({ message: 'refresh expired' }, 401);
  }

  if (authorization === 'Bearer signed.old-access') {
    failedUnauthorizedCalls++;
    if (failedUnauthorizedCalls === 2) releaseFailedRefresh();
    return jsonResponse({ message: 'expired' }, 401);
  }

  return jsonResponse({ ok: true });
}) as typeof fetch;

const concurrentResult = await Promise.race([
  Promise.allSettled([
    client.get('/failed-client'),
    http.get('/api/failed-http')
  ]),
  new Promise<'timeout'>(resolve => setTimeout(() => resolve('timeout'), 250))
]);

if (concurrentResult === 'timeout') {
  throw new Error('并发刷新失败时，所有等待请求都必须结束，不能永久悬空');
}
if (failedRefreshCalls !== 1) {
  throw new Error('两个请求封装的并发刷新失败仍必须只发起一次刷新');
}
if (concurrentResult.some(result => result.status !== 'rejected')) {
  throw new Error('Token 刷新失败时，所有等待请求都必须失败');
}

setStoredToken();
let authEndpointRefreshCalls = 0;
globalThis.fetch = (async (input: RequestInfo | URL) => {
  if (String(input).endsWith('/refresh-token')) authEndpointRefreshCalls++;
  return jsonResponse({ message: 'invalid credentials' }, 401);
}) as typeof fetch;

const loginResult = await Promise.allSettled([
  http.post('/login', { data: { employeeNo: 'invalid', password: 'invalid' } })
]);
if (loginResult[0].status !== 'rejected') {
  throw new Error('登录端点返回 401 时必须直接失败');
}
if (authEndpointRefreshCalls !== 0) {
  throw new Error('登录端点返回 401 时不能使用旧 refreshToken 自动刷新');
}

import { getToken, isLoggedIn, setToken } from './auth';

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

setToken({
  accessToken: 'legacy-random-token',
  refreshToken: 'legacy-refresh-token',
  expires: Date.now() + 60_000
});

if (isLoggedIn()) {
  throw new Error('旧版随机 token 不能继续被前端视为已登录');
}

if (getToken() !== null) {
  throw new Error('旧版随机 token 必须被清理，避免继续触发后端 401');
}

setToken({
  accessToken: 'signed.payload',
  refreshToken: 'signed.refresh',
  expires: Date.now() + 60_000
});

if (!isLoggedIn()) {
  throw new Error('签名 token 在未过期时必须被视为已登录');
}

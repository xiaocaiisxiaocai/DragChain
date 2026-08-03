import { removeToken } from '@/utils/auth';

export function handleAuthFailure() {
  removeToken();

  const current = `${window.location.pathname}${window.location.search}${window.location.hash}`;
  if (window.location.pathname !== '/login') {
    const redirect = encodeURIComponent(current || '/');
    window.location.assign(`/login?redirect=${redirect}`);
  }
}

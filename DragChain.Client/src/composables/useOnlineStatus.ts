import { computed, ref } from 'vue';

const isOnline = ref(navigator.onLine);

/**
 * 监听在线/离线状态
 */
export function useOnlineStatus() {
  function handleOnline() {
    isOnline.value = true;
  }

  function handleOffline() {
    isOnline.value = false;
  }

  window.addEventListener('online', handleOnline);
  window.addEventListener('offline', handleOffline);

  function cleanup() {
    window.removeEventListener('online', handleOnline);
    window.removeEventListener('offline', handleOffline);
  }

  return {
    isOnline: computed(() => isOnline.value),
    cleanup
  };
}

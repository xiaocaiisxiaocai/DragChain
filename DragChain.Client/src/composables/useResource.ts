import { computed, ref } from 'vue';

/**
 * 通用资源管理 Hook
 * @param api - API 对象，必须包含 getAll 方法
 * @returns 资源数据、加载状态和加载函数
 */
export function useResource<T>(api: { getAll: () => Promise<T[]> }) {
  const data = ref<T[]>([]);
  const loading = ref(false);
  const error = ref<Error | null>(null);

  const dataMap = computed(() => {
    if (!Array.isArray(data.value)) return {};
    return data.value.reduce(
      (acc, item: any) => {
        if (item && typeof item === 'object' && 'id' in item) {
          acc[item.id] = item;
        }
        return acc;
      },
      {} as Record<number, T>
    );
  });

  async function load() {
    loading.value = true;
    error.value = null;
    try {
      data.value = await api.getAll();
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err));
      throw err;
    } finally {
      loading.value = false;
    }
  }

  function reset() {
    data.value = [];
    error.value = null;
  }

  return {
    data,
    dataMap,
    loading,
    error,
    load,
    reset
  };
}

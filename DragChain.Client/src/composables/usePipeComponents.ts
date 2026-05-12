import { computed, ref } from 'vue';
import { pipeComponentsApi } from '../api/pipeComponents';
import type { PipeComponent } from '../types';

const pipeComponents = ref<PipeComponent[]>([]);
const componentLoading = ref(false);

export function usePipeComponents() {
  const componentMap = computed(() =>
    pipeComponents.value.reduce(
      (acc, component) => {
        acc[component.id] = component;
        return acc;
      },
      {} as Record<number, PipeComponent>
    )
  );

  async function loadPipeComponents() {
    componentLoading.value = true;
    try {
      pipeComponents.value = await pipeComponentsApi.getAll();
    } finally {
      componentLoading.value = false;
    }
  }

  return {
    pipeComponents,
    componentMap,
    componentLoading,
    loadPipeComponents
  };
}

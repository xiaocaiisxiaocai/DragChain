import { computed, ref } from 'vue';
import { pipeModulesApi } from '../api/pipeModules';
import type { PipeModule } from '../types';

const pipeModules = ref<PipeModule[]>([]);
const moduleLoading = ref(false);

export function usePipeModules() {
  const moduleMap = computed(() =>
    pipeModules.value.reduce(
      (acc, module) => {
        acc[module.id] = module;
        return acc;
      },
      {} as Record<number, PipeModule>
    )
  );

  async function loadPipeModules() {
    moduleLoading.value = true;
    try {
      pipeModules.value = await pipeModulesApi.getAll();
    } finally {
      moduleLoading.value = false;
    }
  }

  return {
    pipeModules,
    moduleMap,
    moduleLoading,
    loadPipeModules
  };
}

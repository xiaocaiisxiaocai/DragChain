import { computed, ref } from 'vue';
import { pipeLibraryApi } from '../api/pipeLibrary';
import type { PipeType } from '../types';

const pipeLib = ref<PipeType[]>([]);
const loading = ref(false);

export function usePipeLibrary() {
  const pipeMap = computed(() =>
    pipeLib.value.reduce(
      (acc, pipe) => {
        acc[pipe.id] = pipe;
        return acc;
      },
      {} as Record<number, PipeType>
    )
  );

  async function loadPipeLib() {
    loading.value = true;
    try {
      pipeLib.value = await pipeLibraryApi.getAll();
    } finally {
      loading.value = false;
    }
  }

  return {
    pipeLib,
    pipeMap,
    loading,
    loadPipeLib
  };
}

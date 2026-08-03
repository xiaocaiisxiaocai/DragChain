import { pipeLibraryApi } from '../api/pipeLibrary';
import type { PipeType } from '../types';
import { useResource } from './useResource';

export function usePipeLibrary() {
  const { data: pipeLib, dataMap: pipeMap, loading, error, load: loadPipeLib, reset } = useResource<PipeType>(pipeLibraryApi);

  return {
    pipeLib,
    pipeMap,
    loading,
    error,
    loadPipeLib,
    reset
  };
}

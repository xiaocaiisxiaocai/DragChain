import { pipeModulesApi } from '../api/pipeModules';
import type { PipeModule } from '../types';
import { useResource } from './useResource';

export function usePipeModules() {
  const {
    data: pipeModules,
    dataMap: moduleMap,
    loading: moduleLoading,
    error,
    load: loadPipeModules,
    reset
  } = useResource<PipeModule>(pipeModulesApi);

  return {
    pipeModules,
    moduleMap,
    moduleLoading,
    error,
    loadPipeModules,
    reset
  };
}

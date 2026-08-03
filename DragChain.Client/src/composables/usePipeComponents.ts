import { pipeComponentsApi } from '../api/pipeComponents';
import type { PipeComponent } from '../types';
import { useResource } from './useResource';

export function usePipeComponents() {
  const {
    data: pipeComponents,
    dataMap: componentMap,
    loading: componentLoading,
    error,
    load: loadPipeComponents,
    reset
  } = useResource<PipeComponent>(pipeComponentsApi);

  return {
    pipeComponents,
    componentMap,
    componentLoading,
    error,
    loadPipeComponents,
    reset
  };
}

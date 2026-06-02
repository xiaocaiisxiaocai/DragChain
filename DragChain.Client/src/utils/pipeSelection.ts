import type { ActivePipe, PipeComponent, PipeModule } from '../types';
import { normalizeLayerMark, type LayerMark } from './layerMark';

export interface ExpandedPipeItem {
  pipeTypeId: number;
  qty: number;
  layer: LayerMark;
}

export function expandSelectionToPipes(
  activePipes: ActivePipe[],
  modules: PipeModule[],
  components: PipeComponent[] = [],
  defaultLayer: LayerMark = 'top'
): ExpandedPipeItem[] {
  const moduleMap = new Map(modules.map(module => [module.id, module]));
  const componentMap = new Map(components.map(component => [component.id, component]));
  const qtyByPipeId = new Map<string, ExpandedPipeItem>();

  function addPipe(pipeTypeId: number, qty: number, layer: string | null | undefined = 'top') {
    if (pipeTypeId <= 0 || qty <= 0) return;
    const normalizedLayer = normalizeLayerMark(layer);
    const key = `${pipeTypeId}-${normalizedLayer}`;
    const existing = qtyByPipeId.get(key);
    qtyByPipeId.set(key, {
      pipeTypeId,
      layer: normalizedLayer,
      qty: (existing?.qty || 0) + qty
    });
  }

  activePipes.forEach(item => {
    if (item.kind === 'module') {
      const module = moduleMap.get(item.moduleId);
      module?.items.forEach(moduleItem => {
        addPipe(moduleItem.pipeTypeId, moduleItem.qty * item.qty, moduleItem.layer);
      });
      return;
    }

    if (item.kind === 'component') {
      const component = componentMap.get(item.componentId);
      component?.items.forEach(componentItem => {
        addPipe(componentItem.pipeTypeId, componentItem.qty * item.qty, componentItem.layer);
      });
      return;
    }

    addPipe(item.libId, item.qty, defaultLayer);
  });

  return [...qtyByPipeId.values()]
    .sort((left, right) => left.pipeTypeId - right.pipeTypeId || left.layer.localeCompare(right.layer));
}

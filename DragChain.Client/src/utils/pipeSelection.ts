import type { ActivePipe, PipeComponent, PipeModule } from '../types';

export interface ExpandedPipeItem {
  pipeTypeId: number;
  qty: number;
}

export function expandSelectionToPipes(
  activePipes: ActivePipe[],
  modules: PipeModule[],
  components: PipeComponent[] = []
): ExpandedPipeItem[] {
  const moduleMap = new Map(modules.map(module => [module.id, module]));
  const componentMap = new Map(components.map(component => [component.id, component]));
  const qtyByPipeId = new Map<number, number>();

  function addPipe(pipeTypeId: number, qty: number) {
    if (pipeTypeId <= 0 || qty <= 0) return;
    qtyByPipeId.set(pipeTypeId, (qtyByPipeId.get(pipeTypeId) || 0) + qty);
  }

  activePipes.forEach(item => {
    if (item.kind === 'module') {
      const module = moduleMap.get(item.moduleId);
      module?.items.forEach(moduleItem => {
        addPipe(moduleItem.pipeTypeId, moduleItem.qty * item.qty);
      });
      return;
    }

    if (item.kind === 'component') {
      const component = componentMap.get(item.componentId);
      component?.items.forEach(componentItem => {
        addPipe(componentItem.pipeTypeId, componentItem.qty * item.qty);
      });
      return;
    }

    addPipe(item.libId, item.qty);
  });

  return [...qtyByPipeId.entries()]
    .sort(([left], [right]) => left - right)
    .map(([pipeTypeId, qty]) => ({ pipeTypeId, qty }));
}

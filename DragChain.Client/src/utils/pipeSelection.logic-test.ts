import { expandSelectionToPipes } from './pipeSelection';
import type { ActivePipe, PipeComponent, PipeModule } from '../types';

const selections: ActivePipe[] = [
  { kind: 'pipe', libId: 1, qty: 2 },
  { kind: 'module', moduleId: 10, qty: 3 },
  { kind: 'component', componentId: 20, qty: 2 }
];

const modules: PipeModule[] = [
  {
    id: 10,
    name: '标准伺服包',
    description: '',
    items: [
      { id: 1, moduleId: 10, pipeTypeId: 1, qty: 1, layer: 'top' },
      { id: 2, moduleId: 10, pipeTypeId: 2, qty: 2, layer: 'top' }
    ]
  }
];

const components: PipeComponent[] = [
  {
    id: 20,
    name: '标准阀组',
    description: '',
    items: [
      { id: 3, componentId: 20, pipeTypeId: 2, qty: 1, layer: 'bottom' },
      { id: 4, componentId: 20, pipeTypeId: 3, qty: 4, layer: 'top' }
    ]
  }
];

const expanded = expandSelectionToPipes(selections, modules, components);

const expected: Array<{ pipeTypeId: number; layer: 'top' | 'bottom'; qty: number }> = [
  { pipeTypeId: 1, layer: 'top', qty: 5 },
  { pipeTypeId: 2, layer: 'bottom', qty: 2 },
  { pipeTypeId: 2, layer: 'top', qty: 6 },
  { pipeTypeId: 3, layer: 'top', qty: 8 }
];

const assertExpanded: typeof expected = expanded;

if (JSON.stringify(assertExpanded) !== JSON.stringify(expected)) {
  throw new Error('模块和元件展开结果不符合预期');
}

const bottomSinglePipe = expandSelectionToPipes([{ kind: 'pipe', libId: 1, qty: 1 }], modules, components, 'bottom');
if (bottomSinglePipe.length !== 1 || bottomSinglePipe[0].layer !== 'bottom') {
  throw new Error('单根管线必须按所在槽位层作为默认上下标识');
}

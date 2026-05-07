import { expandSelectionToPipes } from './pipeSelection';
import type { ActivePipe, PipeModule } from '../types';

const selections: ActivePipe[] = [
  { kind: 'pipe', libId: 1, qty: 2 },
  { kind: 'module', moduleId: 10, qty: 3 }
];

const modules: PipeModule[] = [
  {
    id: 10,
    name: '标准伺服包',
    description: '',
    items: [
      { id: 1, moduleId: 10, pipeTypeId: 1, qty: 1 },
      { id: 2, moduleId: 10, pipeTypeId: 2, qty: 2 }
    ]
  }
];

const expanded = expandSelectionToPipes(selections, modules);

const expected: Array<{ pipeTypeId: number; qty: number }> = [
  { pipeTypeId: 1, qty: 5 },
  { pipeTypeId: 2, qty: 6 }
];

const assertExpanded: typeof expected = expanded;

if (JSON.stringify(assertExpanded) !== JSON.stringify(expected)) {
  throw new Error('模块展开结果不符合预期');
}

import { createTrunkingSelectionRows } from './trunkingSelectionDisplay';
import type { ActivePipe, PipeComponent, PipeModule, PipeType } from '../types';

const pipeLib: PipeType[] = [
  { id: 1, name: '传感器信号电缆', type: 'weak_cable', diameter: 5, weight: 0.12, bendMultiplier: 8 },
  { id: 2, name: '伺服电源线', type: 'strong_cable', diameter: 10, weight: 0.2, bendMultiplier: 6 },
  { id: 3, name: '气管 Φ10', type: 'tube', diameter: 10, weight: 0.2, bendMultiplier: 6 }
];

const activePipes: ActivePipe[] = [
  { kind: 'pipe', libId: 1, qty: 2 },
  { kind: 'module', moduleId: 10, qty: 3 },
  { kind: 'component', componentId: 20, qty: 2 }
];

const modules: PipeModule[] = [
  {
    id: 10,
    name: '阀岛模块',
    description: '',
    items: [
      { id: 1, moduleId: 10, pipeTypeId: 1, qty: 1 },
      { id: 2, moduleId: 10, pipeTypeId: 2, qty: 2 },
      { id: 3, moduleId: 10, pipeTypeId: 3, qty: 1 }
    ]
  }
];

const components: PipeComponent[] = [
  {
    id: 20,
    name: '阀组元件',
    description: '',
    items: [
      { id: 4, componentId: 20, pipeTypeId: 1, qty: 2 },
      { id: 5, componentId: 20, pipeTypeId: 2, qty: 1 }
    ]
  }
];

const rows = createTrunkingSelectionRows(activePipes, pipeLib, modules, components);

const singlePipe = rows[0];
if (singlePipe.sizeText !== 'Φ5 mm' || singlePipe.areaText !== '50') {
  throw new Error('单根管线必须显示尺寸和按数量汇总的面积');
}
if (singlePipe.typeLabel !== '弱电电缆' || singlePipe.sideLabel !== '左侧') {
  throw new Error('单根管线必须显示分类和侧别');
}

const moduleRow = rows[1];
if (moduleRow.sizeText !== '最大 Φ10 mm' || moduleRow.areaText !== '975') {
  throw new Error('模块行必须显示最大尺寸和按模块数量汇总的面积');
}
if (moduleRow.typeLabel !== '模块' || moduleRow.sideLabel !== '混合') {
  throw new Error('模块行必须显示类型和侧别');
}

if (moduleRow.children.length !== 3 || moduleRow.children[1].areaText !== '600' || moduleRow.children[1].qty !== 6) {
  throw new Error('模块展开详情必须显示子管线尺寸、面积和展开后总数量');
}
if (moduleRow.children[1].typeLabel !== '强电电缆' || moduleRow.children[1].sideLabel !== '右侧') {
  throw new Error('模块子项必须显示强电和右侧');
}
if (moduleRow.children[2].typeLabel !== '气管' || moduleRow.children[2].sideLabel !== '左侧') {
  throw new Error('模块子项必须显示气管和左侧');
}

const componentRow = rows[2];
if (componentRow.typeLabel !== '元件' || componentRow.sideLabel !== '混合') {
  throw new Error('元件行必须显示类型和侧别');
}
if (componentRow.children.length !== 2 || componentRow.children[0].unitQtyText !== '2/元件' || componentRow.children[0].qty !== 4) {
  throw new Error('元件展开详情必须显示子管线和展开后总数量');
}

const chainRows = createTrunkingSelectionRows(activePipes, pipeLib, modules, components, { areaMode: 'circle' });
if (chainRows[0].areaText !== '39.3' || chainRows[1].areaText !== '765.8' || chainRows[2].areaText !== '235.6') {
  throw new Error('拖链清单面积必须按圆形截面积显示');
}

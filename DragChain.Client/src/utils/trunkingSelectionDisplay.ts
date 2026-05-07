import type { ActivePipe, PipeModule, PipeType } from '../types';

export interface TrunkingSelectionDetailRow {
  selectionKey: string;
  kind: 'module-item';
  name: string;
  typeLabel: string;
  sideLabel: string;
  qty: number;
  unitQtyText: string;
  sizeText: string;
  areaText: string;
  canExpand: false;
}

export interface TrunkingSelectionRow {
  selectionKey: string;
  sourceIndex: number;
  kind: 'pipe' | 'module';
  name: string;
  typeLabel: string;
  sideLabel: string;
  qty: number;
  detail: string;
  sizeText: string;
  areaText: string;
  canExpand: boolean;
  children: TrunkingSelectionDetailRow[];
}

export interface TrunkingSelectionDisplayOptions {
  areaMode?: 'square' | 'circle';
}

export function createTrunkingSelectionRows(
  activePipes: ActivePipe[],
  pipeLib: PipeType[],
  modules: PipeModule[],
  options: TrunkingSelectionDisplayOptions = {}
): TrunkingSelectionRow[] {
  const pipeMap = new Map(pipeLib.map(pipe => [pipe.id, pipe]));
  const moduleMap = new Map(modules.map(module => [module.id, module]));
  const calcArea = options.areaMode === 'circle' ? calcCircleArea : calcSquareArea;

  return activePipes
    .map((selection, sourceIndex): TrunkingSelectionRow | null => {
      if (selection.kind === 'module') {
        const module = moduleMap.get(selection.moduleId);
        if (!module) return null;

        const children = module.items.map(item => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          const totalQty = item.qty * selection.qty;
          const area = pipe && totalQty > 0 ? calcArea(pipe.diameter, totalQty) : 0;

          return {
            selectionKey: `module-${sourceIndex}-item-${item.pipeTypeId}-${item.id}`,
            kind: 'module-item' as const,
            name: pipe?.name || `#${item.pipeTypeId}`,
            typeLabel: getPipeTypeLabel(pipe?.type),
            sideLabel: getSideLabel(pipe?.type),
            qty: totalQty,
            unitQtyText: `${item.qty}/模块`,
            sizeText: pipe ? formatDiameter(pipe.diameter) : '-',
            areaText: area > 0 ? formatArea(area) : '-',
            canExpand: false as const
          };
        });

        const maxDiameter = module.items.reduce((max, item) => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          return Math.max(max, pipe?.diameter || 0);
        }, 0);
        const area = module.items.reduce((sum, item) => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          return pipe && selection.qty > 0 ? sum + calcArea(pipe.diameter, item.qty * selection.qty) : sum;
        }, 0);

        return {
          ...baseRow(selection, sourceIndex),
          kind: 'module',
          name: module.name,
          typeLabel: '模块',
          sideLabel: '混合',
          detail: children.map(item => `${item.name}×${item.unitQtyText}`).join('，'),
          sizeText: maxDiameter > 0 ? `最大 ${formatDiameter(maxDiameter)}` : '-',
          areaText: area > 0 ? formatArea(area) : '-',
          canExpand: children.length > 0,
          children
        };
      }

      const pipe = pipeMap.get(selection.libId);
      if (!pipe) return null;

      const area = selection.qty > 0 ? calcArea(pipe.diameter, selection.qty) : 0;
      return {
        ...baseRow(selection, sourceIndex),
        kind: 'pipe',
        name: pipe.name,
        typeLabel: getPipeTypeLabel(pipe.type),
        sideLabel: getSideLabel(pipe.type),
        detail: '-',
        sizeText: formatDiameter(pipe.diameter),
        areaText: area > 0 ? formatArea(area) : '-',
        canExpand: false,
        children: []
      };
    })
    .filter((row): row is TrunkingSelectionRow => row !== null);
}

function baseRow(selection: ActivePipe, sourceIndex: number) {
  return {
    selectionKey: `${selection.kind || 'pipe'}-${sourceIndex}`,
    sourceIndex,
    qty: selection.qty
  };
}

function formatDiameter(value: number) {
  return `Φ${formatNumber(value)} mm`;
}

function formatArea(value: number) {
  return Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1);
}

function formatNumber(value: number) {
  return Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1);
}

function calcSquareArea(diameter: number, qty: number) {
  return diameter * diameter * qty;
}

function calcCircleArea(diameter: number, qty: number) {
  return Math.PI * Math.pow(diameter / 2, 2) * qty;
}

function getPipeTypeLabel(type?: string) {
  return type === 'strong_cable' ? '强电电缆'
    : type === 'weak_cable' || type === 'cable' ? '弱电电缆'
    : type === 'encoder' ? '编码器'
    : type === 'tube' ? '气管'
    : '其他';
}

function getSideLabel(type?: string) {
  return type === 'strong_cable' ? '右侧' : '左侧';
}

import type { ActivePipe, PipeComponent, PipeModule, PipeType } from '../types';

export interface TrunkingSelectionDetailRow {
  selectionKey: string;
  kind: 'module-item';
  name: string;
  typeLabel: string;
  layerLabel: string;
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
  kind: 'pipe' | 'module' | 'component';
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

export interface TrunkingSelectionAreaSummary {
  totalArea: number;
  leftArea: number;
  rightArea: number;
  totalAreaText: string;
  leftAreaText: string;
  rightAreaText: string;
}

export function createTrunkingSelectionRows(
  activePipes: ActivePipe[],
  pipeLib: PipeType[],
  modules: PipeModule[],
  componentsOrOptions: PipeComponent[] | TrunkingSelectionDisplayOptions = [],
  options: TrunkingSelectionDisplayOptions = {}
): TrunkingSelectionRow[] {
  const components = Array.isArray(componentsOrOptions) ? componentsOrOptions : [];
  const displayOptions = Array.isArray(componentsOrOptions) ? options : componentsOrOptions;
  const pipeMap = new Map(pipeLib.map(pipe => [pipe.id, pipe]));
  const moduleMap = new Map(modules.map(module => [module.id, module]));
  const componentMap = new Map(components.map(component => [component.id, component]));
  const calcArea = displayOptions.areaMode === 'circle' ? calcCircleArea : calcSquareArea;

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
            layerLabel: getLayerLabel(item.layer),
            sideLabel: getSideLabel(pipe?.type),
            qty: totalQty,
            unitQtyText: formatNumber(item.qty),
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

      if (selection.kind === 'component') {
        const component = componentMap.get(selection.componentId);
        if (!component) return null;

        const children = component.items.map(item => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          const totalQty = item.qty * selection.qty;
          const area = pipe && totalQty > 0 ? calcArea(pipe.diameter, totalQty) : 0;

          return {
            selectionKey: `component-${sourceIndex}-item-${item.pipeTypeId}-${item.id}`,
            kind: 'module-item' as const,
            name: pipe?.name || `#${item.pipeTypeId}`,
            typeLabel: getPipeTypeLabel(pipe?.type),
            layerLabel: getLayerLabel(item.layer),
            sideLabel: getSideLabel(pipe?.type),
            qty: totalQty,
            unitQtyText: formatNumber(item.qty),
            sizeText: pipe ? formatDiameter(pipe.diameter) : '-',
            areaText: area > 0 ? formatArea(area) : '-',
            canExpand: false as const
          };
        });

        const maxDiameter = component.items.reduce((max, item) => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          return Math.max(max, pipe?.diameter || 0);
        }, 0);
        const area = component.items.reduce((sum, item) => {
          const pipe = pipeMap.get(item.pipeTypeId) || item.pipeType;
          return pipe && selection.qty > 0 ? sum + calcArea(pipe.diameter, item.qty * selection.qty) : sum;
        }, 0);

        return {
          ...baseRow(selection, sourceIndex),
          kind: 'component',
          name: component.name,
          typeLabel: '元件',
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

export function summarizeTrunkingSelectionRows(rows: TrunkingSelectionRow[]): TrunkingSelectionAreaSummary {
  let leftArea = 0;
  let rightArea = 0;

  rows.forEach(row => {
    if (row.children.length > 0) {
      row.children.forEach(child => {
        if (child.sideLabel === '右侧') rightArea += parseAreaText(child.areaText);
        else leftArea += parseAreaText(child.areaText);
      });
      return;
    }

    if (row.sideLabel === '右侧') rightArea += parseAreaText(row.areaText);
    else leftArea += parseAreaText(row.areaText);
  });

  const totalArea = leftArea + rightArea;

  return {
    totalArea,
    leftArea,
    rightArea,
    totalAreaText: formatSummaryArea(totalArea),
    leftAreaText: formatSummaryArea(leftArea),
    rightAreaText: formatSummaryArea(rightArea)
  };
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

function formatSummaryArea(value: number) {
  if (value <= 0) return '0';
  return formatArea(value).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

function parseAreaText(value: string) {
  const area = Number(value.replace(/,/g, ''));
  return Number.isFinite(area) ? area : 0;
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

function getLayerLabel(layer?: string | null) {
  return layer === 'bottom' ? '下' : '上';
}

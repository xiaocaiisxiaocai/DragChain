import type { ActivePipe } from './pipe';

// 线槽型录
export interface TrunkingCatalog {
  id: number;
  model: string;
  width: number;
  height: number;
  crossSection: number;
  fillRatioLimit: number;
}

// 线槽计算请求
export interface TrunkingCalcRequest {
  selectedTrunkingId: number;
  pipes: { pipeTypeId: number; qty: number; layer?: 'top' | 'bottom' }[];
  fillRatio?: number;
  /** 缺省值仅用于兼容旧保存数据，按 topToBottom 解释。 */
  slotOrder?: 'bottomToTop' | 'topToBottom';
  slots?: TrunkingSlotRequest[];
}

export interface TrunkingSlotRequest {
  id: string;
  name: string;
  layout: 'ordered' | 'leftRight' | 'topBottom';
  leftTrunkingId?: number | null;
  rightTrunkingId?: number | null;
  leftFillRatio?: number | null;
  rightFillRatio?: number | null;
  pipes?: { pipeTypeId: number; qty: number; layer?: 'top' | 'bottom' }[];
  sections?: TrunkingSlotSectionRequest[];
}

export interface TrunkingSlotSectionRequest {
  key: 'top' | 'bottom' | string;
  label: string;
  selectedTrunkingId?: number | null;
  fillRatio?: number | null;
  pipes: { pipeTypeId: number; qty: number; layer?: 'top' | 'bottom' }[];
}

export interface TrunkingSettings {
  fillRatio: number;
}

export interface TrunkingSavedSelection {
  id?: string;
  name: string;
  savedAt?: string;
  request: TrunkingCalcRequest;
  result: TrunkingCalcResponse | null;
  sourceSlots?: TrunkingSavedSourceSlot[];
}

export interface TrunkingSavedSourceSlot {
  id: string;
  name: string;
  sections: TrunkingSavedSourceSection[];
}

export interface TrunkingSavedSourceSection {
  key: 'top' | 'bottom';
  label: string;
  pipes: ActivePipe[];
}

// 线槽计算步骤
export interface TrunkingSteps {
  step1_TotalArea: string;
  step1_MaxDia: string;
  step1_PipeCount: string;
  step2_TrunkingArea: string;
  step2_FillRatio: string;
  step3_Result: string;
}

// 线槽计算响应
export interface TrunkingCalcResponse {
  totalArea: number;
  fillRatio: number;
  actualFillRatio: number;
  maxPipeDia: number;
  totalPipeCount: number;
  selectedTrunking: TrunkingCatalog | null;
  matchResults: TrunkingMatchResult[];
  weakSide: TrunkingSideResult | null;
  strongSide: TrunkingSideResult | null;
  slots: TrunkingSlotResult[];
  sideSlots: TrunkingSlotResult[];
  steps: TrunkingSteps;
  resultStatus: 'ok' | 'warn' | 'err';
  resultMessage: string;
}

export interface TrunkingSlotResult {
  id: string;
  name: string;
  layout: 'ordered' | 'leftRight' | 'topBottom';
  sections: TrunkingSideResult[];
  resultStatus: 'ok' | 'warn' | 'err';
  resultMessage: string;
}

export interface TrunkingSideResult {
  key: 'weak' | 'strong' | 'left' | 'right' | 'top' | 'bottom' | string;
  label: string;
  totalArea: number;
  fillRatio: number;
  actualFillRatio: number;
  maxPipeDia: number;
  totalPipeCount: number;
  selectedTrunking: TrunkingCatalog | null;
  matchResults: TrunkingMatchResult[];
  resultStatus: 'ok' | 'warn' | 'err';
  resultMessage: string;
}

export interface TrunkingMatchResult {
  id: number;
  model: string;
  width: number;
  height: number;
  crossSection: number;
  fillRatioLimit: number;
  actualFillRatio: number;
  okFill: boolean;
  isRecommended: boolean;
  result: string;
}

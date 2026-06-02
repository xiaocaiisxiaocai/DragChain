// 管线类型
export interface PipeType {
  id: number;
  name: string;
  type: 'tube' | 'weak_cable' | 'strong_cable' | 'cable' | 'encoder' | 'other';
  diameter: number;
  weight: number;
  bendMultiplier: number;
}

// 活动管线（管线清单）
export interface ActivePipeBase {
  kind?: 'pipe' | 'module' | 'component';
  qty: number;
}

export interface ActivePipeItem extends ActivePipeBase {
  kind?: 'pipe';
  libId: number;
  name?: string;
  type?: string;
  diameter?: number;
  weight?: number;
  bendMultiplier?: number;
}

export interface ActivePipeModule extends ActivePipeBase {
  kind: 'module';
  moduleId: number;
}

export interface ActivePipeComponent extends ActivePipeBase {
  kind: 'component';
  componentId: number;
}

export type ActivePipe = ActivePipeItem | ActivePipeModule | ActivePipeComponent;

// 管线模块
export interface PipeModuleItem {
  id: number;
  moduleId: number;
  pipeTypeId: number;
  qty: number;
  layer: 'top' | 'bottom';
  pipeType?: PipeType;
}

export interface PipeModule {
  id: number;
  name: string;
  description: string;
  items: PipeModuleItem[];
}

// 管线元件
export interface PipeComponentItem {
  id: number;
  componentId: number;
  pipeTypeId: number;
  qty: number;
  layer: 'top' | 'bottom';
  pipeType?: PipeType;
}

export interface PipeComponent {
  id: number;
  name: string;
  description: string;
  items: PipeComponentItem[];
}

// WZL 型录
export interface WzlCatalog {
  id: number;
  model: string;
  function: string;
  stroke: string;
  innerHeight: number;
  innerWidth: number;
  outerHeight: number;
  outerWidth: number;
  minRadius: number;
  recRadius: number;
  reservedK: number;
  bendLength: number;
  mountingH1: string;
  interferenceH2: string;
  innerArea: number | null;
  appPipes: string;
}

// ME 型录
export interface MeCatalog {
  id: number;
  baseModel: string;
  functionSelect: string;
  innerHeight: number;
  innerWidth: number;
  r1: number;
  r2: number;
  r3: number;
  r1Suffix: string;
  r2Suffix: string;
  r3Suffix: string;
  lp1: number;
  lp2: number;
  lp3: number;
  mountingH1: string;
  innerArea: number;
  maxWeight: number;
  spanBase: number;
  spanSlope: number;
}

// 计算请求
export interface CalculationRequest {
  brand: 'wzl' | 'me';
  sensorCount: number;
  magnetCount: number;
  motionType: '横移' | '升降';
  stroke: number;
  lmOffset: number;
  pipes: { pipeTypeId: number; qty: number }[];
}

// 计算结果 — 匹配行
export interface MatchResult {
  model: string;
  innerHeight: number;
  recRadius: number;
  innerArea: number;
  calcSpan: number;
  okHeight: boolean;
  okRadius: boolean;
  okArea: boolean;
  okPrelim: boolean;
  okSpan: boolean;
  okFinal: boolean;
}

// 计算结果 — 选定型号
export interface SelectedModel {
  model: string;
  lp: number;
  lk: number;
  recRadius: number;
  innerArea: number;
}

// 计算结果 — 计算步骤
export interface CalculationSteps {
  step3_1_MinHeight: string;
  step3_2_BendTube: string;
  step3_2_BendCable: string;
  step3_2_BendMax: string;
  step3_3_AreaSum: string;
  step3_3_Ratio: string;
  step3_3_MinArea: string;
  step3_4_PrelimModel: string;
  step3_5_Motion: string;
  step3_5_Stroke: string;
  step3_5_Lm: string;
  step3_5_PrelimLp: string;
  step3_5_PrelimLk: string;
  step3_5_PrelimFull: string;
  step3_6_NeedSpan: string;
  step3_6_Load: string;
  step3_6_SpanOk: string;
  step3_6_FinalModel: string;
  step3_6_FinalLp: string;
  step3_6_FinalLk: string;
}

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

// 计算响应
export interface CalculationResponse {
  minHeight: number;
  minRadius: number;
  totalArea: number;
  minInnerArea: number;
  totalWeight: number;
  needSpan: number;
  coreCount: number;
  tubeBend: number;
  cableBend: number;
  encoderBend: number;
  matchResults: MatchResult[];
  preliminaryModel: SelectedModel | null;
  finalModel: SelectedModel | null;
  steps: CalculationSteps;
  resultStatus: 'ok' | 'warn' | 'err';
  resultMessage: string;
  strategyNote: string | null;
}

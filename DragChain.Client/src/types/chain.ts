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

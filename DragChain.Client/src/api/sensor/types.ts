/** 传感器类型 */
export interface SensorType {
  id: string;
  name: string;
}

/** 产品 */
export interface Product {
  id: number;
  code: string;
  model: string;
  name: string;
  brand: string;
  type: string;
  spec?: string;
  scene?: string;
}

export interface ProductReplacementNodePreview {
  nodeId: number;
  nodeName: string;
  resultCount: number;
}

export interface ProductReplacementResultPreview {
  resultId: number;
  nodeId: number;
  nodeName: string;
  note?: string | null;
  oldQuantity: number;
  existingNewQuantity: number;
  finalNewQuantity: number;
  willMerge: boolean;
}

export interface ProductReplacementPreview {
  oldProduct: Pick<Product, "id" | "code" | "model" | "name" | "type">;
  newProduct: Pick<Product, "id" | "code" | "model" | "name" | "type">;
  affectedResultCount: number;
  affectedNodeCount: number;
  mergeResultCount: number;
  affectedNodes: ProductReplacementNodePreview[];
  affectedResults: ProductReplacementResultPreview[];
}

/** 制程注意事项 */
export interface ProcessNote {
  id: number;
  processName: string;
  characteristic: string;
  selectionNote: string;
}

/** 功能条件 */
export interface FunctionCondition {
  id: number;
  code: string;
  name: string;
  note?: string;
}

/** 场景功能 */
export interface ScenarioFunction {
  id: number;
  code: string;
  name: string;
  icon: string;
  note?: string;
  conditions: FunctionCondition[];
}

/** 机构场景 */
export interface Scenario {
  id: number;
  code: string;
  name: string;
  icon: string;
  desc?: string;
  functions: ScenarioFunction[];
}

/** 规则产品 */
export interface RuleProductItem {
  productId: number;
  productCode: string;
  productModel: string;
  productName?: string;
  productType: string;
  quantity: number;
}

/** 选型结果 */
export interface SelectionResult {
  id: number;
  nodeId?: number;
  nodeName?: string;
  note?: string;
  imageUrl?: string | null;
  sortOrder: number;
  products: RuleProductItem[];
}

/** 受影响机构 */
export interface AffectedMechanism {
  id: number;
  mechanismCode: string;
  mechanismName: string;
  changeDesc?: string;
  changeDescDetail?: string;
  changeDescDetail2?: string;
  installNote?: string;
  condition?: string;
  relatedConditions?: string;
}

/** 制程场景 */
export interface ProcessScenario {
  id: number;
  code: string;
  name: string;
  icon: string;
  desc?: string;
  sopSource?: string;
  category?: string;
  affectedMechanisms: AffectedMechanism[];
  unaffectedMechanisms: { id: number; mechanismCode: string }[];
}

/** 创建/编辑规则DTO */
export interface RuleProductDto {
  productId: number;
  quantity: number;
}

export interface SelectionResultDto {
  note?: string;
  sortOrder: number;
  products: RuleProductDto[];
}

export interface SelectionTreeNode {
  id: number;
  entryId: number;
  parentId?: number | null;
  code: string;
  name: string;
  nodeType: string;
  icon?: string;
  description?: string;
  sortOrder: number;
  isLeaf: boolean;
  children: SelectionTreeNode[];
}

export interface SaveSelectionNodeDto {
  parentId?: number | null;
  name: string;
}

/** 场景/功能/条件维护 DTO */
export interface CreateScenarioDto {
  code: string;
  name: string;
  icon: string;
  desc?: string;
}

export interface UpdateScenarioDto {
  name: string;
  icon: string;
  desc?: string;
  sortOrder: number;
}

export interface CreateFunctionDto {
  code: string;
  name: string;
  icon: string;
  note?: string;
  scenarioId: number;
}

export interface UpdateFunctionDto {
  name: string;
  icon: string;
  note?: string;
  scenarioId: number;
  sortOrder: number;
}

export interface CreateConditionDto {
  code: string;
  name: string;
  note?: string;
  functionId: number;
}

export interface UpdateConditionDto {
  name: string;
  note?: string;
  functionId: number;
  sortOrder: number;
}

export interface ReorderItem {
  id: number;
  sortOrder: number;
}

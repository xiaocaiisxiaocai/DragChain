import { http } from "@/utils/http";
import type {
  Product,
  ProductReplacementPreview,
  ProcessNote,
  Scenario,
  SelectionResult,
  SelectionResultDto,
  SelectionTreeNode,
  SaveSelectionNodeDto,
  ProcessScenario,
  SensorType,
  CreateScenarioDto,
  UpdateScenarioDto,
  CreateFunctionDto,
  UpdateFunctionDto,
  CreateConditionDto,
  UpdateConditionDto,
  ReorderItem
} from "./types";

/** 获取产品列表 */
export const getProducts = (type?: string, keyword?: string) =>
  http.get<Product[], void>("/api/products", { params: { type, keyword } });

/** 获取单个产品 */
export const getProduct = (id: number) =>
  http.get<Product, void>(`/api/products/${id}`);

/** 创建产品 */
export const createProduct = (data: Product) =>
  http.post<Product, Product>("/api/products", { data });

/** 更新产品 */
export const updateProduct = (id: number, data: Product) =>
  http.request<void>("put", `/api/products/${id}`, { data });

/** 删除产品 */
export const deleteProduct = (id: number) =>
  http.request<void>("delete", `/api/products/${id}`);

/** 预览一键替换感应器影响范围 */
export const getProductReplacementPreview = (id: number, newProductId: number) =>
  http.get<ProductReplacementPreview, void>(`/api/products/${id}/replacement-preview`, { params: { newProductId } });

/** 一键替换选型结果中的感应器 */
export const replaceSelectionResultProduct = (id: number, newProductId: number) =>
  http.post<ProductReplacementPreview, { newProductId: number }>(
    `/api/products/${id}/replace-selection-results`,
    { data: { newProductId } }
  );

/** 导出产品 */
export const exportProducts = (type?: string) =>
  http.request<Blob>(
    "get",
    "/api/products/export",
    { params: { type }, responseType: "blob" },
    {
      beforeResponseCallback: (response: { data: Blob }) => response.data
    }
  );

/** 导入产品 */
export const importProducts = (data: FormData) =>
  http.post<{ created: number; updated: number; total: number }, FormData>(
    "/api/products/import",
    { data },
    { headers: { "Content-Type": "multipart/form-data" } }
  );

/** 获取制程注意事项 */
export const getProcessNotes = (keyword?: string) =>
  http.get<ProcessNote[], void>("/api/process-notes", { params: { keyword } });

/** 创建制程注意事项 */
export const createProcessNote = (data: ProcessNote) =>
  http.post<ProcessNote, ProcessNote>("/api/process-notes", { data });

/** 更新制程注意事项 */
export const updateProcessNote = (id: number, data: ProcessNote) =>
  http.request<void>("put", `/api/process-notes/${id}`, { data });

/** 删除制程注意事项 */
export const deleteProcessNote = (id: number) =>
  http.request<void>("delete", `/api/process-notes/${id}`);

/** 获取场景树 */
export const getScenarios = () =>
  http.get<Scenario[], void>("/api/scenarios");

/** 获取选型分类树 */
export const getSelectionTree = () =>
  http.get<SelectionTreeNode[], void>("/api/selection-tree");

/** 创建选型分类 */
export const createSelectionNode = (data: SaveSelectionNodeDto) =>
  http.post<SelectionTreeNode, SaveSelectionNodeDto>("/api/selection-tree/nodes", { data });

/** 更新选型分类 */
export const updateSelectionNode = (id: number, data: SaveSelectionNodeDto) =>
  http.request<void>("put", `/api/selection-tree/nodes/${id}`, { data });

/** 删除选型分类 */
export const deleteSelectionNode = (id: number) =>
  http.request<void>("delete", `/api/selection-tree/nodes/${id}`);

/** 获取分类选型结果 */
export const getSelectionResults = (nodeId: number) =>
  http.get<SelectionResult[], void>(`/api/selection-tree/nodes/${nodeId}/results`);

/** 全局搜索选型结果 */
export const searchSelectionResults = (keyword?: string) =>
  http.get<SelectionResult[], void>("/api/selection-tree/results/search", { params: { keyword } });

/** 创建分类选型结果 */
export const createSelectionResult = (nodeId: number, data: SelectionResultDto) =>
  http.post<SelectionResult, SelectionResultDto>(`/api/selection-tree/nodes/${nodeId}/results`, { data });

/** 更新分类选型结果 */
export const updateSelectionResult = (resultId: number, data: SelectionResultDto) =>
  http.request<void>("put", `/api/selection-tree/results/${resultId}`, { data });

/** 删除分类选型结果 */
export const deleteSelectionResult = (resultId: number) =>
  http.request<void>("delete", `/api/selection-tree/results/${resultId}`);

/** 删除选型结果图片 */
export const deleteSelectionResultImage = (resultId: number) =>
  http.request<void>("delete", `/api/selection-tree/results/${resultId}/image`);

/** 上传选型结果图片 */
export const uploadSelectionResultImage = (resultId: number, data: FormData) =>
  http.post<{ imageUrl: string }, FormData>(
    `/api/selection-tree/results/${resultId}/image`,
    { data },
    { headers: { "Content-Type": "multipart/form-data" } }
  );

/** 获取制程场景 */
export const getProcessScenarios = () =>
  http.get<ProcessScenario[], void>("/api/process-scenarios");

/** 获取传感器类型 */
export const getSensorTypes = (keyword?: string) =>
  http.get<SensorType[], void>("/api/sensor-types", { params: { keyword } });

// === 传感器类型 CRUD ===
export const createSensorType = (data: SensorType) =>
  http.post<SensorType, SensorType>("/api/sensor-types", { data });

export const updateSensorType = (id: string, data: SensorType) =>
  http.request<void>("put", `/api/sensor-types/${id}`, { data });

export const deleteSensorType = (id: string) =>
  http.request<void>("delete", `/api/sensor-types/${id}`);

// === 场景 CRUD ===
export const createScenario = (data: CreateScenarioDto) =>
  http.post<void, CreateScenarioDto>("/api/scenarios", { data });

export const updateScenario = (id: number, data: UpdateScenarioDto) =>
  http.request<void>("put", `/api/scenarios/${id}`, { data });

export const deleteScenario = (id: number) =>
  http.request<void>("delete", `/api/scenarios/${id}`);

export const reorderScenarios = (items: ReorderItem[]) =>
  http.request<void>("put", "/api/scenarios/reorder", { data: items });

// === 功能 CRUD ===
export const getScenarioFunctions = (scenarioId?: number) =>
  http.get<any, void>("/api/scenario-functions", { params: { scenarioId } });

export const createFunction = (data: CreateFunctionDto) =>
  http.post<void, CreateFunctionDto>("/api/scenario-functions", { data });

export const updateFunction = (id: number, data: UpdateFunctionDto) =>
  http.request<void>("put", `/api/scenario-functions/${id}`, { data });

export const deleteFunction = (id: number) =>
  http.request<void>("delete", `/api/scenario-functions/${id}`);

export const reorderFunctions = (items: ReorderItem[]) =>
  http.request<void>("put", "/api/scenario-functions/reorder", { data: items });

// === 条件 CRUD ===
export const getFunctionConditions = (functionId?: number) =>
  http.get<any, void>("/api/function-conditions", { params: { functionId } });

export const createCondition = (data: CreateConditionDto) =>
  http.post<void, CreateConditionDto>("/api/function-conditions", { data });

export const updateCondition = (id: number, data: UpdateConditionDto) =>
  http.request<void>("put", `/api/function-conditions/${id}`, { data });

export const deleteCondition = (id: number) =>
  http.request<void>("delete", `/api/function-conditions/${id}`);

export const reorderConditions = (items: ReorderItem[]) =>
  http.request<void>("put", "/api/function-conditions/reorder", { data: items });

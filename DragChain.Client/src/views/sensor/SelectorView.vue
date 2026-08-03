<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import {
  ElButton,
  ElDialog,
  ElEmpty,
  ElForm,
  ElFormItem,
  ElInput,
  ElInputNumber,
  ElImage,
  ElMessage,
  ElMessageBox,
  ElTag,
  ElTooltip,
  ElTree,
  ElUpload
} from "element-plus";
import type { UploadRequestOptions } from "element-plus";
import AddLine from "~icons/ri/add-line";
import DeleteBinLine from "~icons/ri/delete-bin-line";
import EditLine from "~icons/ri/edit-line";
import ImageAddLine from "~icons/ri/image-add-line";
import Save3Line from "~icons/ri/save-3-line";
import SearchLine from "~icons/ri/search-line";
import {
  createSelectionNode,
  createSelectionResult,
  deleteSelectionNode,
  deleteSelectionResult,
  deleteSelectionResultImage,
  getProducts,
  getSelectionResults,
  getSelectionTree,
  searchSelectionResults,
  updateSelectionNode,
  updateSelectionResult,
  uploadSelectionResultImage
} from "@/api/sensor";
import type {
  Product,
  SaveSelectionNodeDto,
  SelectionResult,
  SelectionResultDto,
  SelectionTreeNode
} from "@/api/sensor/types";
import { authVersion, hasPerms } from "@/utils/auth";
import { debounce } from "@/utils/debounce";
import { createLatestAsync } from "@/utils/latestAsync";
import { UPLOAD_LIMITS } from "@/constants";

defineOptions({ name: "SensorSelector" });

const route = useRoute();
const tree = ref<SelectionTreeNode[]>([]);
const treeRef = ref<InstanceType<typeof ElTree>>();
const selectedNode = ref<SelectionTreeNode | null>(null);
const results = ref<SelectionResult[]>([]);
const globalResults = ref<SelectionResult[]>([]);
const products = ref<Product[]>([]);
const loadingResults = ref(false);
const treeKeyword = ref("");
const globalKeyword = ref("");
const searchingResults = ref(false);
const layoutRef = ref<HTMLElement | null>(null);
const treePanelWidth = ref(380);
const treePanelMinWidth = 280;
const treePanelDefaultWidth = 380;
const treePanelMaxWidth = 680;
const resultPanelMinWidth = 520;
const noopStopTreeResize = () => undefined;
let stopTreeResize = noopStopTreeResize;

const nodeDialogVisible = ref(false);
const nodeDialogTitle = ref("添加分类");
const editingNodeId = ref<number | null>(null);
const nodeForm = ref<SaveSelectionNodeDto>({
  parentId: null,
  name: ""
});

const resultDialogVisible = ref(false);
const resultDialogTitle = ref("添加选型结果");
const editingResultId = ref<number | null>(null);
const resultForm = ref<SelectionResultDto>({
  note: "",
  sortOrder: 1,
  products: []
});

const selectedProductIds = computed(() => new Set(resultForm.value.products.map(item => item.productId)));
const normalizedTreeKeyword = computed(() => treeKeyword.value.trim().toLowerCase());
const normalizedGlobalKeyword = computed(() => globalKeyword.value.trim().toLowerCase());
const isGlobalSearchMode = computed(() => normalizedGlobalKeyword.value.length > 0);
const filteredTree = computed(() => filterTree(tree.value, normalizedTreeKeyword.value));
const treeRenderKey = computed(() => normalizedTreeKeyword.value ? `search-${normalizedTreeKeyword.value}` : "normal");
const resultHighlightKeyword = computed(() => isGlobalSearchMode.value ? globalKeyword.value.trim() : "");
const defaultExpandedTreeKeys = computed(() => {
  if (normalizedTreeKeyword.value) return collectExpandableNodeIds(filteredTree.value);
  const selectedPath = selectedNode.value ? collectNodePathIds(tree.value, selectedNode.value.id) : [];
  return selectedPath.slice(0, -1);
});
const displayResults = computed(() => isGlobalSearchMode.value ? globalResults.value : results.value);
const isPublicHome = computed(() => route.path === "/" || route.name === "HomeSelector");
const canEditSelector = computed(() => {
  authVersion.value;
  return hasPerms("api:selector:write") && !isPublicHome.value;
});
const resultPanelTitle = computed(() =>
  isGlobalSearchMode.value ? `全局搜索：${globalKeyword.value.trim()}` : selectedNode.value?.name || "未选择分类"
);
const resultPanelSubtitle = computed(() => {
  if (isGlobalSearchMode.value) return `共找到 ${globalResults.value.length} 条匹配结果`;
  return "点击左侧分类查看或维护选型结果";
});

onMounted(async () => {
  await loadPage();
});

onBeforeUnmount(() => {
  stopTreeResize();
  debouncedLoadGlobalResults.cancel();
  globalSearch.invalidate();
});

const globalSearch = createLatestAsync(
  (keyword: string) => searchSelectionResults(keyword),
  searchResults => {
    globalResults.value = searchResults;
  },
  loading => {
    searchingResults.value = loading;
  }
);

const debouncedLoadGlobalResults = debounce((keyword: string) => {
  void globalSearch.run(keyword).catch(error => {
    ElMessage.error(error instanceof Error ? error.message : "搜索失败");
  });
}, 500);

watch(globalKeyword, value => {
  const keyword = value.trim();
  debouncedLoadGlobalResults.cancel();
  globalSearch.invalidate();
  if (!keyword) {
    globalResults.value = [];
    return;
  }
  debouncedLoadGlobalResults(keyword);
});

async function loadPage() {
  const [treeData, productData] = await Promise.all([
    getSelectionTree(),
    getProducts()
  ]);
  tree.value = treeData;
  products.value = productData;
  if (!selectedNode.value) selectedNode.value = findFirstLeaf(treeData) ?? treeData[0] ?? null;
  if (selectedNode.value) await loadResults(selectedNode.value.id);
}

async function loadTree() {
  tree.value = await getSelectionTree();
  if (selectedNode.value) selectedNode.value = findNode(tree.value, selectedNode.value.id);
}

async function loadResults(nodeId: number) {
  loadingResults.value = true;
  try {
    results.value = await getSelectionResults(nodeId);
  } finally {
    loadingResults.value = false;
  }
}

async function refreshGlobalResults() {
  if (!isGlobalSearchMode.value) return;
  const keyword = globalKeyword.value.trim();
  if (!keyword) return;

  debouncedLoadGlobalResults.cancel();
  globalSearch.invalidate();
  await globalSearch.run(keyword);
}

async function handleSelectNode(
  node: SelectionTreeNode,
  treeNode?: { expanded: boolean; expand: () => void; collapse: () => void }
) {
  if (node.children.length > 0) {
    if (treeNode?.expanded) {
      treeNode.collapse();
    } else {
      treeNode?.expand();
    }
  }

  selectedNode.value = node;
  globalKeyword.value = "";
  await loadResults(node.id);
}

function handleAddRoot() {
  if (!ensureCanEdit()) return;
  openNodeDialog(null);
}

function handleAddChild(node: SelectionTreeNode) {
  if (!ensureCanEdit()) return;
  openNodeDialog(node);
}

function handleEditNode(node: SelectionTreeNode) {
  if (!ensureCanEdit()) return;
  editingNodeId.value = node.id;
  nodeDialogTitle.value = "编辑分类";
  nodeForm.value = {
    parentId: node.parentId ?? null,
    name: node.name
  };
  nodeDialogVisible.value = true;
}

function openNodeDialog(parent: SelectionTreeNode | null) {
  editingNodeId.value = null;
  nodeDialogTitle.value = parent ? `添加子分类 - ${parent.name}` : "添加分类";
  nodeForm.value = {
    parentId: parent?.id ?? null,
    name: ""
  };
  nodeDialogVisible.value = true;
}

async function handleSaveNode() {
  if (!ensureCanEdit()) return;
  if (!nodeForm.value.name.trim()) {
    ElMessage.warning("请填写分类名称");
    return;
  }
  if (editingNodeId.value) {
    await updateSelectionNode(editingNodeId.value, nodeForm.value);
    ElMessage.success("分类已更新");
  } else {
    await createSelectionNode(nodeForm.value);
    ElMessage.success("分类已添加");
  }
  nodeDialogVisible.value = false;
  await loadTree();
}

async function handleDeleteNode(node: SelectionTreeNode) {
  if (!ensureCanEdit()) return;
  try {
    await ElMessageBox.confirm(`确定删除分类「${node.name}」吗？`, "确认删除", { type: "warning" });
    await deleteSelectionNode(node.id);
    ElMessage.success("分类已删除");
    if (selectedNode.value?.id === node.id) {
      selectedNode.value = null;
      results.value = [];
    }
    await loadTree();
  } catch (error: any) {
    if (error?.response?.data?.message) ElMessage.error(error.response.data.message);
  }
}

function handleAddResult() {
  if (!ensureCanEdit()) return;
  if (!selectedNode.value) return;
  editingResultId.value = null;
  resultDialogTitle.value = "添加选型结果";
  resultForm.value = { note: "", sortOrder: results.value.length + 1, products: [] };
  resultDialogVisible.value = true;
}

function handleEditResult(row: SelectionResult) {
  if (!ensureCanEdit()) return;
  editingResultId.value = row.id;
  resultDialogTitle.value = "编辑选型结果";
  resultForm.value = {
    note: row.note || "",
    sortOrder: row.sortOrder,
    products: row.products.map(item => ({ productId: item.productId, quantity: item.quantity }))
  };
  resultDialogVisible.value = true;
}

async function handleSaveResult() {
  if (!ensureCanEdit()) return;
  if (!selectedNode.value) return;
  if (editingResultId.value) {
    try {
      await updateSelectionResult(editingResultId.value, resultForm.value);
      ElMessage.success("选型结果已更新");
    } catch (error: any) {
      ElMessage.error(error?.response?.data?.message || "保存失败");
      return;
    }
  } else {
    try {
      await createSelectionResult(selectedNode.value.id, resultForm.value);
      ElMessage.success("选型结果已添加");
    } catch (error: any) {
      ElMessage.error(error?.response?.data?.message || "保存失败");
      return;
    }
  }
  resultDialogVisible.value = false;
  await loadResults(selectedNode.value.id);
  await refreshGlobalResults();
}

async function handleDeleteResult(row: SelectionResult) {
  if (!ensureCanEdit()) return;
  try {
    await ElMessageBox.confirm("确定删除这条选型结果吗？", "确认删除", { type: "warning" });
    await deleteSelectionResult(row.id);
    ElMessage.success("选型结果已删除");
    if (selectedNode.value) await loadResults(selectedNode.value.id);
    await refreshGlobalResults();
  } catch { /* cancelled */ }
}

async function handleUploadResultImage(row: SelectionResult, options: UploadRequestOptions) {
  if (!ensureCanEdit()) return;
  const file = options.file;

  // 检查文件类型
  if (!UPLOAD_LIMITS.IMAGE_MIME_TYPES.includes(file.type as any)) {
    ElMessage.warning("请选择图片文件（支持 PNG、JPEG、WebP 格式）");
    return;
  }

  // 检查文件大小
  if (file.size > UPLOAD_LIMITS.IMAGE_MAX_SIZE) {
    const sizeMB = (UPLOAD_LIMITS.IMAGE_MAX_SIZE / 1024 / 1024).toFixed(0);
    ElMessage.warning(`图片大小不能超过 ${sizeMB}MB`);
    return;
  }

  const form = new FormData();
  form.append("file", file);

  try {
    await uploadSelectionResultImage(row.id, form);
    ElMessage.success("图片已更新");
    if (selectedNode.value) await loadResults(selectedNode.value.id);
    await refreshGlobalResults();
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : "图片上传失败");
  }
}

async function handleDeleteResultImage(row: SelectionResult) {
  if (!ensureCanEdit()) return;
  try {
    await ElMessageBox.confirm("确定删除这张图片吗？", "确认删除", { type: "warning" });
    await deleteSelectionResultImage(row.id);
    ElMessage.success("图片已删除");
    if (selectedNode.value) await loadResults(selectedNode.value.id);
    await refreshGlobalResults();
  } catch { /* cancelled */ }
}

function toggleProduct(productId: number) {
  if (!ensureCanEdit()) return;
  const exists = resultForm.value.products.find(item => item.productId === productId);
  if (exists) {
    resultForm.value.products = resultForm.value.products.filter(item => item.productId !== productId);
  } else {
    resultForm.value.products.push({ productId, quantity: 1 });
  }
}

function setProductQuantity(productId: number, quantity: number) {
  if (!ensureCanEdit()) return;
  const item = resultForm.value.products.find(item => item.productId === productId);
  if (item) item.quantity = Math.max(quantity || 1, 1);
}

function ensureCanEdit() {
  if (canEditSelector.value) return true;
  ElMessage.warning("请先登录后再编辑");
  return false;
}

function getProductQuantity(productId: number) {
  return resultForm.value.products.find(item => item.productId === productId)?.quantity ?? 1;
}

function renderProductLabel(productId: number) {
  const product = products.value.find(item => item.id === productId);
  return product ? formatProductDisplayName(product.model, product.name) : `产品 ${productId}`;
}

type HighlightPart = {
  text: string;
  matched: boolean;
};

function splitHighlightText(value: string | number | null | undefined, keyword: string): HighlightPart[] {
  const text = String(value ?? "");
  const term = keyword.trim();
  if (!text || !term) return [{ text, matched: false }];

  const textLower = text.toLowerCase();
  const termLower = term.toLowerCase();
  const parts: HighlightPart[] = [];
  let start = 0;

  while (start < text.length) {
    const index = textLower.indexOf(termLower, start);
    if (index === -1) break;
    if (index > start) parts.push({ text: text.slice(start, index), matched: false });
    parts.push({ text: text.slice(index, index + term.length), matched: true });
    start = index + term.length;
  }

  if (start < text.length) parts.push({ text: text.slice(start), matched: false });
  return parts.length ? parts : [{ text, matched: false }];
}

function findNode(nodes: SelectionTreeNode[], id: number): SelectionTreeNode | null {
  for (const node of nodes) {
    if (node.id === id) return node;
    const child = findNode(node.children, id);
    if (child) return child;
  }
  return null;
}

function findFirstLeaf(nodes: SelectionTreeNode[]): SelectionTreeNode | null {
  for (const node of nodes) {
    if (node.children.length === 0) return node;
    const child = findFirstLeaf(node.children);
    if (child) return child;
  }
  return null;
}

function filterTree(nodes: SelectionTreeNode[], keyword: string): SelectionTreeNode[] {
  if (!keyword) return nodes;
  return nodes
    .map(node => {
      const children = filterTree(node.children, keyword);
      const matched = node.name.toLowerCase().includes(keyword);
      return matched || children.length ? { ...node, children } : null;
    })
    .filter((node): node is SelectionTreeNode => !!node);
}

function collectExpandableNodeIds(nodes: SelectionTreeNode[]): number[] {
  return nodes.flatMap(node => [
    ...(node.children.length ? [node.id] : []),
    ...collectExpandableNodeIds(node.children)
  ]);
}

function collectNodePathIds(nodes: SelectionTreeNode[], id: number): number[] {
  for (const node of nodes) {
    if (node.id === id) return [node.id];
    const childPath = collectNodePathIds(node.children, id);
    if (childPath.length) return [node.id, ...childPath];
  }
  return [];
}

function productSummary(row: SelectionResult) {
  return row.products.length
    ? row.products.map(item => `${item.productModel} x${item.quantity}`).join("、")
    : "待维护产品";
}

function resultProductTitle(row: SelectionResult) {
  return row.products.length
    ? row.products.map(formatProductName).join(" / ")
    : "待维护产品";
}

function resultProductTags(row: SelectionResult) {
  return row.products.map(item => ({
    key: `${row.id}-${item.productId}`,
    label: `${formatProductName(item)} x${item.quantity}`,
    type: item.productType
}));
}

function formatProductName(item: SelectionResult["products"][number]) {
  return formatProductDisplayName(item.productModel, item.productName) || `产品 ${item.productId}`;
}

function formatProductDisplayName(model?: string | null, name?: string | null) {
  const productModel = model?.trim() || "";
  const productName = getReadableProductName(productModel, name);
  const names = [productModel, productName].filter(Boolean);
  return Array.from(new Set(names)).join(" ");
}

function getReadableProductName(model: string, name?: string | null) {
  const productNameFallbacks: Record<string, string> = {
    "FAB-18D16N1-D3": "标准近接开关"
  };
  const value = name?.trim() || "";
  if (value && !isBrokenProductText(value)) return value;
  return productNameFallbacks[model] || "";
}

function isBrokenProductText(value: string) {
  return /\?{2,}/.test(value);
}

function getTreePanelMaxWidth() {
  const layoutWidth = layoutRef.value?.clientWidth ?? 0;
  if (!layoutWidth) return treePanelMaxWidth;
  return Math.max(treePanelMinWidth, Math.min(treePanelMaxWidth, layoutWidth - resultPanelMinWidth));
}

function clampTreePanelWidth(width: number) {
  return Math.min(Math.max(width, treePanelMinWidth), getTreePanelMaxWidth());
}

function handleTreeResizeStart(event: PointerEvent) {
  if (event.button !== 0) return;
  const startX = event.clientX;
  const startWidth = treePanelWidth.value;

  const handlePointerMove = (moveEvent: PointerEvent) => {
    treePanelWidth.value = clampTreePanelWidth(startWidth + moveEvent.clientX - startX);
  };
  const handlePointerUp = () => stopTreeResize();

  stopTreeResize();
  document.body.classList.add("selector-resizing");
  window.addEventListener("pointermove", handlePointerMove);
  window.addEventListener("pointerup", handlePointerUp, { once: true });
  window.addEventListener("pointercancel", handlePointerUp, { once: true });

  stopTreeResize = () => {
    document.body.classList.remove("selector-resizing");
    window.removeEventListener("pointermove", handlePointerMove);
    window.removeEventListener("pointerup", handlePointerUp);
    window.removeEventListener("pointercancel", handlePointerUp);
    stopTreeResize = noopStopTreeResize;
  };
}

function resetTreePanelWidth() {
  treePanelWidth.value = clampTreePanelWidth(treePanelDefaultWidth);
}

type TagType = "primary" | "success" | "warning" | "danger" | "info";

function getTypeBadge(type: string): TagType {
  const map: Record<string, TagType> = {
    proximity_18: "warning", proximity_30: "warning", proximity_large: "warning",
    proximity_flush: "warning", proximity: "warning",
    "photoelectric-bg": "primary", photoelectric: "primary", diffuse: "primary", reflective: "primary",
    capacitive: "success", capacitive_small: "success",
    slot: "danger", fiber_m6: "danger", fiber_m3: "danger", fiber: "danger",
    laser: "warning", grating: "danger", switch: "primary", lock: "primary",
    color_sensor: "primary", vacuum_gauge: "primary"
  };
  return map[type] || "primary";
}
</script>

<template>
  <div class="sensor-console sensor-selector-page">
    <div class="selector-global-search">
      <el-input v-model="globalKeyword" clearable placeholder="全局搜索分类 / 产品 / 作用">
        <template #prefix>
          <SearchLine />
        </template>
      </el-input>
    </div>

    <section
      ref="layoutRef"
      class="selector-layout"
      :style="{ gridTemplateColumns: `${treePanelWidth}px 8px minmax(0, 1fr)` }"
    >
      <aside class="selector-tree-panel">
        <div class="panel-toolbar">
          <div class="panel-title">分类</div>
          <div v-if="canEditSelector" class="panel-actions">
            <el-button size="small" @click="handleAddRoot"><AddLine />新增顶层</el-button>
          </div>
        </div>
        <el-input v-model="treeKeyword" clearable placeholder="搜索分类" class="tree-search">
          <template #prefix>
            <SearchLine />
          </template>
        </el-input>
        <el-tree
          ref="treeRef"
          :key="treeRenderKey"
          :data="filteredTree"
          node-key="id"
          :props="{ label: 'name', children: 'children' }"
          :default-expanded-keys="defaultExpandedTreeKeys"
          :expand-on-click-node="false"
          empty-text="暂无匹配分类"
          highlight-current
          class="selector-tree"
          @node-click="handleSelectNode"
        >
          <template #default="{ data }">
            <span class="tree-node-label">
              <el-tooltip :content="data.name" placement="top" :show-after="400">
                <span class="tree-node-name">
                  <span
                    v-for="(part, index) in splitHighlightText(data.name, treeKeyword)"
                    :key="`${data.id}-name-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </span>
              </el-tooltip>
              <span v-if="canEditSelector" class="tree-node-actions" @click.stop>
                <el-tooltip content="添加子级" placement="top" :show-after="300">
                  <el-button
                    class="tree-action-button"
                    size="small"
                    type="primary"
                    link
                    aria-label="添加子级"
                    @click.stop="handleAddChild(data)"
                  >
                    <AddLine />
                  </el-button>
                </el-tooltip>
                <el-tooltip content="编辑分类" placement="top" :show-after="300">
                  <el-button
                    class="tree-action-button"
                    size="small"
                    type="primary"
                    link
                    aria-label="编辑分类"
                    @click.stop="handleEditNode(data)"
                  >
                    <EditLine />
                  </el-button>
                </el-tooltip>
                <el-tooltip content="删除分类" placement="top" :show-after="300">
                  <el-button
                    class="tree-action-button"
                    size="small"
                    type="danger"
                    link
                    aria-label="删除分类"
                    @click.stop="handleDeleteNode(data)"
                  >
                    <DeleteBinLine />
                  </el-button>
                </el-tooltip>
              </span>
            </span>
          </template>
        </el-tree>
      </aside>

      <div
        class="selector-resize-handle"
        title="拖动调整分类栏宽度，双击恢复默认宽度"
        @pointerdown="handleTreeResizeStart"
        @dblclick="resetTreePanelWidth"
      />

      <main class="selector-result-panel">
        <div class="panel-toolbar">
          <div>
            <div class="panel-title">{{ resultPanelTitle }}</div>
            <div class="panel-subtitle">{{ resultPanelSubtitle }}</div>
          </div>
          <el-button
            v-if="canEditSelector"
            type="primary"
            :disabled="isGlobalSearchMode || !selectedNode"
            @click="handleAddResult"
          >
            <AddLine />
            添加选型结果
          </el-button>
        </div>

        <el-empty v-if="!isGlobalSearchMode && !selectedNode" description="请选择左侧分类" />
        <div v-else v-loading="loadingResults || searchingResults" class="selector-result-body">
          <el-empty
            v-if="!displayResults.length"
            :description="isGlobalSearchMode ? '暂无匹配结果' : '暂无选型结果'"
          />
          <div v-else class="result-list">
            <article v-for="row in displayResults" :key="row.id" class="result-card">
              <div class="result-main">
                <div class="result-head">
                  <div class="result-note">
                    <span
                      v-for="(part, index) in splitHighlightText(row.note || '未填写作用', resultHighlightKeyword)"
                      :key="`${row.id}-note-${index}`"
                      :class="{ 'keyword-highlight': part.matched }"
                    >
                      {{ part.text }}
                    </span>
                  </div>
                  <el-tag size="small" type="info">排序 {{ row.sortOrder }}</el-tag>
                </div>
                <div class="result-sensors">
                  <div v-if="isGlobalSearchMode" class="result-node">
                    分类：
                    <span
                      v-for="(part, index) in splitHighlightText(row.nodeName || '-', resultHighlightKeyword)"
                      :key="`${row.id}-node-${index}`"
                      :class="{ 'keyword-highlight': part.matched }"
                    >
                      {{ part.text }}
                    </span>
                  </div>
                  <div class="result-tags">
                    <el-tag
                      v-for="tag in resultProductTags(row)"
                      :key="tag.key"
                      size="small"
                      :type="getTypeBadge(tag.type)"
                    >
                      <span
                        v-for="(part, index) in splitHighlightText(tag.label, resultHighlightKeyword)"
                        :key="`${tag.key}-label-${index}`"
                        :class="{ 'keyword-highlight': part.matched }"
                      >
                        {{ part.text }}
                      </span>
                    </el-tag>
                  </div>
                </div>
                <div v-if="canEditSelector" class="result-actions">
                  <el-button size="small" type="primary" link @click="handleEditResult(row)">
                    <EditLine />
                    编辑
                  </el-button>
                  <el-button size="small" type="danger" link @click="handleDeleteResult(row)">
                    <DeleteBinLine />
                    删除
                  </el-button>
                </div>
              </div>

              <div class="result-image-panel">
                <el-image
                  v-if="row.imageUrl"
                  :src="row.imageUrl"
                  :preview-src-list="[row.imageUrl]"
                  preview-teleported
                  fit="cover"
                  class="result-thumb"
                />
                <div v-else class="result-image-empty">
                  <ImageAddLine />
                </div>
                <div v-if="canEditSelector" class="result-image-actions">
                  <el-upload
                    :show-file-list="false"
                    accept="image/png,image/jpeg,image/webp"
                    :http-request="options => handleUploadResultImage(row, options)"
                  >
                    <el-button size="small" class="image-upload-button">
                      <ImageAddLine />
                      {{ row.imageUrl ? "更换" : "上传" }}
                    </el-button>
                  </el-upload>
                  <el-button
                    v-if="row.imageUrl"
                    size="small"
                    type="danger"
                    plain
                    class="image-delete-button"
                    @click="handleDeleteResultImage(row)"
                  >
                    <DeleteBinLine />
                    删除
                  </el-button>
                </div>
              </div>
            </article>
          </div>
        </div>
      </main>
    </section>

    <el-dialog
      v-model="nodeDialogVisible"
      class="selector-dialog selector-node-dialog"
      :title="nodeDialogTitle"
      width="min(480px, calc(100vw - 24px))"
    >
      <el-form :model="nodeForm" label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="nodeForm.name" placeholder="分类名称" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="nodeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveNode"><Save3Line />保存</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="resultDialogVisible"
      class="selector-dialog selector-result-dialog"
      :title="resultDialogTitle"
      width="min(760px, calc(100vw - 24px))"
    >
      <el-form :model="resultForm" label-width="80px">
        <el-form-item label="作用">
          <el-input v-model="resultForm.note" type="textarea" :rows="3" placeholder="填写作用说明" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="resultForm.sortOrder" :min="1" />
        </el-form-item>
        <el-form-item label="产品">
          <div class="product-picker">
            <div v-for="product in products" :key="product.id" class="product-row">
              <el-button
                size="small"
                :type="selectedProductIds.has(product.id) ? 'primary' : 'default'"
                @click="toggleProduct(product.id)"
              >
                {{ renderProductLabel(product.id) }}
              </el-button>
              <el-input-number
                v-if="selectedProductIds.has(product.id)"
                :model-value="getProductQuantity(product.id)"
                :min="1"
                size="small"
                @change="(value?: number) => setProductQuantity(product.id, value ?? 1)"
              />
            </div>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resultDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveResult"><Save3Line />保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
@use "./shared-ui.scss";

.sensor-selector-page {
  display: flex;
  height: calc(100vh - 129px);
  min-height: 0;
  overflow: hidden;
  flex-direction: column;
}

.selector-global-search {
  flex: 0 0 auto;
  width: min(520px, 100%);
  margin-bottom: 12px;
}

.selector-global-search :deep(.el-input__wrapper),
.tree-search :deep(.el-input__wrapper) {
  border-radius: 6px;
}

.selector-layout {
  display: grid;
  gap: 8px;
  flex: 1 1 auto;
  min-height: 0;
}

.selector-tree-panel,
.selector-result-panel {
  min-height: 0;
  padding: 16px;
  overflow: hidden;
  background: var(--sensor-color-surface);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
  box-shadow: var(--sensor-shadow-sm);
}

.selector-tree-panel,
.selector-result-panel {
  display: flex;
  flex-direction: column;
}

.selector-resize-handle {
  position: relative;
  width: 8px;
  min-height: 0;
  cursor: col-resize;
  touch-action: none;
}

.selector-resize-handle::before {
  position: absolute;
  top: 8px;
  bottom: 8px;
  left: 3px;
  width: 2px;
  content: "";
  background: #cbd5e1;
  border-radius: 999px;
  transition:
    width 0.15s ease,
    background 0.15s ease,
    box-shadow 0.15s ease;
}

.selector-resize-handle:hover::before {
  left: 2px;
  width: 4px;
  background: var(--sensor-color-primary);
  box-shadow: 0 0 0 3px #dbeafe;
}

:global(body.selector-resizing) {
  cursor: col-resize;
  user-select: none;
}

.panel-toolbar {
  flex: 0 0 auto;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}

.panel-title {
  font-size: 16px;
  font-weight: 800;
  color: var(--sensor-color-text);
}

.panel-subtitle {
  margin-top: 4px;
  font-size: 12px;
  color: var(--sensor-color-muted);
}

.panel-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 6px;
}

.tree-search {
  flex: 0 0 auto;
  margin-bottom: 12px;
}

.selector-tree {
  --el-tree-node-hover-bg-color: var(--sensor-color-primary-soft);
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
  padding-right: 4px;
}

.selector-tree :deep(.el-tree-node__content) {
  height: 34px;
  min-width: 0;
  padding-right: 6px;
}

:global(.selector-dialog .el-dialog__header) {
  padding: 20px 28px 14px !important;
}

:global(.selector-dialog .el-dialog__body) {
  padding: 16px 28px 18px !important;
}

:global(.selector-dialog .el-dialog__footer) {
  padding: 16px 28px !important;
}

.selector-tree :deep(.el-tree-node__label) {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.tree-node-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  width: 100%;
  min-width: 0;
}

.tree-node-name {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tree-node-actions {
  display: inline-flex;
  flex: 0 0 82px;
  justify-content: flex-end;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.15s ease;
}

.tree-action-button {
  width: 22px;
  height: 22px;
  min-height: 22px !important;
  padding: 0 !important;
}

.tree-action-button :deep(svg) {
  width: 14px;
  height: 14px;
}

.keyword-highlight {
  padding: 0 2px;
  color: #92400e;
  background: #fef3c7;
  border-radius: 3px;
}

.tree-node-label:hover .tree-node-actions,
.selector-tree :deep(.el-tree-node.is-current > .el-tree-node__content) .tree-node-actions {
  opacity: 1;
}

.selector-result-body {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
  padding-right: 4px;
}

.result-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.result-card {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 220px;
  gap: 16px;
  align-items: stretch;
  padding: 12px;
  background: var(--sensor-color-surface);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
  transition:
    border-color 160ms ease,
    box-shadow 160ms ease;
}

.result-card:hover {
  border-color: #bfdbfe;
  box-shadow: var(--sensor-shadow-sm);
}

.result-image-panel {
  display: flex;
  width: 220px;
  min-width: 0;
  flex-direction: column;
  gap: 8px;
  justify-self: end;
}

.result-thumb,
.result-image-empty {
  width: 220px;
  height: 150px;
  border-radius: 6px;
}

.result-image-empty {
  display: grid;
  place-items: center;
  color: #94a3b8;
  background: var(--sensor-color-bg);
  border: 1px dashed #cbd5e1;
}

.result-image-empty :deep(svg) {
  width: 32px;
  height: 32px;
}

.result-image-actions {
  display: flex;
  gap: 8px;
}

.result-image-actions :deep(.el-upload) {
  flex: 1 1 0;
  min-width: 0;
  width: 100%;
}

.image-upload-button {
  width: 100%;
  padding: 0 8px;
}

.image-delete-button {
  flex: 1 1 0;
  min-width: 0;
  width: 100%;
  padding: 0 8px;
}

.image-upload-button :deep(svg),
.image-delete-button :deep(svg) {
  width: 14px;
  height: 14px;
  margin-right: 4px;
}

.result-main {
  display: flex;
  min-width: 0;
  flex-direction: column;
}

.result-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 10px;
}

.result-sensors {
  min-width: 0;
}

.result-node {
  margin-bottom: 8px;
  overflow: hidden;
  font-size: 12px;
  color: var(--sensor-color-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.result-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.result-tags :deep(.el-tag) {
  height: 26px;
  padding: 0 10px;
  font-size: 14px;
  font-weight: 800;
}

.result-note {
  display: block;
  flex: 1 1 auto;
  overflow: hidden;
  font-size: 15px;
  font-weight: 800;
  line-height: 1.7;
  color: var(--sensor-color-text);
  overflow-wrap: anywhere;
  white-space: pre-line;
}

.result-actions {
  display: flex;
  flex: 1 1 auto;
  gap: 8px;
  align-items: flex-end;
  justify-content: flex-end;
  padding-top: 10px;
  white-space: nowrap;
}

.product-picker {
  display: flex;
  max-height: 360px;
  width: 100%;
  flex-direction: column;
  gap: 8px;
  overflow: auto;
  padding-right: 4px;
}

.product-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
  align-items: center;
}

.product-row :deep(.el-button) {
  justify-content: flex-start;
  overflow: hidden;
}

@media (max-width: 900px) {
  .sensor-selector-page {
    height: auto;
    min-height: calc(100vh - 105px);
    overflow: visible;
  }

  .selector-layout {
    grid-template-columns: 1fr;
  }

  .selector-layout[style] {
    grid-template-columns: 1fr !important;
  }

  .selector-resize-handle {
    display: none;
  }

  .selector-tree-panel,
  .selector-result-panel {
    min-height: auto;
    overflow: visible;
  }

  .selector-tree,
  .selector-result-body {
    overflow: visible;
  }

  .panel-toolbar {
    flex-direction: column;
  }

  .result-card {
    grid-template-columns: minmax(0, 1fr);
  }

  .result-image-panel,
  .result-thumb,
  .result-image-empty {
    width: 100%;
  }

  .result-image-panel {
    justify-self: stretch;
  }

  .result-thumb,
  .result-image-empty {
    height: 180px;
  }

  .result-actions {
    justify-content: flex-end;
  }
}
</style>

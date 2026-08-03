<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import { ElTable, ElTableColumn, ElButton, ElDialog, ElForm, ElFormItem, ElInput, ElSelect, ElOption, ElTag, ElMessage, ElMessageBox, ElTabs, ElTabPane } from "element-plus";
import AddLine from "~icons/ri/add-line";
import DeleteBinLine from "~icons/ri/delete-bin-line";
import Download2Line from "~icons/ri/download-2-line";
import EditLine from "~icons/ri/edit-line";
import ExchangeLine from "~icons/ri/exchange-line";
import Save3Line from "~icons/ri/save-3-line";
import SearchLine from "~icons/ri/search-line";
import Upload2Line from "~icons/ri/upload-2-line";
import { getProducts, createProduct, updateProduct, deleteProduct, exportProducts, importProducts, getProductReplacementPreview, replaceSelectionResultProduct, getProcessNotes, createProcessNote, updateProcessNote, deleteProcessNote, getSensorTypes, createSensorType, updateSensorType, deleteSensorType } from "@/api/sensor";
import type { Product, ProductReplacementPreview, ProcessNote, SensorType } from "@/api/sensor/types";

defineOptions({ name: "SensorProducts" });

const activeTab = ref("products");
const products = ref<Product[]>([]);
const replacementCandidates = ref<Product[]>([]);
const processNotes = ref<ProcessNote[]>([]);
const sensorTypes = ref<SensorType[]>([]);
const typeOptions = ref<SensorType[]>([]);
const typeFilter = ref("");
const productKeyword = ref("");
const typeKeyword = ref("");
const processNoteKeyword = ref("");
const importInputRef = ref<HTMLInputElement>();
const importing = ref(false);
const exporting = ref(false);
const dialogVisible = ref(false);
const dialogTitle = ref("添加产品");
const form = ref<Product>({ id: 0, code: "", model: "", name: "", brand: "", type: "", spec: "", scene: "" });
const replaceDialogVisible = ref(false);
const replacing = ref(false);
const loadingReplacementCandidates = ref(false);
const loadingReplacementPreview = ref(false);
const replacingProduct = ref<Product | null>(null);
const replacementProductId = ref<number | null>(null);
const replacementPreview = ref<ProductReplacementPreview | null>(null);

// 制程注意事项维护
const noteDialogVisible = ref(false);
const noteDialogTitle = ref("添加制程注意事项");
const noteForm = ref<ProcessNote>({ id: 0, processName: "", characteristic: "", selectionNote: "" });

// 类型维护
const typeDialogVisible = ref(false);
const typeDialogTitle = ref("添加类型");
const typeForm = ref<{ id: string; name: string; _editing?: boolean }>({ id: "", name: "" });

const filteredProducts = computed(() => {
  return products.value;
});

function isUnauthorized(error: any) {
  return error?.response?.status === 401;
}

onMounted(async () => {
  await loadData();
});

watch(typeFilter, async () => {
  await loadProducts();
});

async function loadData() {
  try {
    const [p, t, n] = await Promise.all([
      getProducts(typeFilter.value || undefined, productKeyword.value.trim() || undefined),
      getSensorTypes(typeKeyword.value.trim() || undefined),
      getProcessNotes(processNoteKeyword.value.trim() || undefined)
    ]);
    products.value = p;
    sensorTypes.value = t;
    typeOptions.value = await getSensorTypes();
    processNotes.value = n;
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
  }
}

async function loadProducts() {
  try {
    products.value = await getProducts(typeFilter.value || undefined, productKeyword.value.trim() || undefined);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
  }
}

async function loadSensorTypes() {
  try {
    sensorTypes.value = await getSensorTypes(typeKeyword.value.trim() || undefined);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
  }
}

async function loadTypeOptions() {
  try {
    typeOptions.value = await getSensorTypes();
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
  }
}

async function loadProcessNotes() {
  try {
    processNotes.value = await getProcessNotes(processNoteKeyword.value.trim() || undefined);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
  }
}

function getTypeName(type: string) {
  return typeOptions.value.find(t => t.id === type)?.name || type;
}

const replacementOptions = computed(() =>
  replacementCandidates.value.filter(product => product.id !== replacingProduct.value?.id)
);

const replacementResultGroups = computed(() => {
  const groups = new Map<number, { nodeId: number; nodeName: string; results: ProductReplacementPreview["affectedResults"] }>();
  for (const result of replacementPreview.value?.affectedResults || []) {
    const group = groups.get(result.nodeId);
    if (group) {
      group.results.push(result);
    } else {
      groups.set(result.nodeId, {
        nodeId: result.nodeId,
        nodeName: result.nodeName || `分类 ${result.nodeId}`,
        results: [result]
      });
    }
  }
  return Array.from(groups.values());
});

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

// 产品操作
function handleAdd() {
  dialogTitle.value = "添加产品";
  form.value = { id: 0, code: "", model: "", name: "", brand: "", type: "", spec: "", scene: "" };
  dialogVisible.value = true;
}

function handleEdit(row: Product) {
  dialogTitle.value = "编辑产品";
  form.value = { ...row };
  dialogVisible.value = true;
}

function formatProductLabel(product: Product | ProductReplacementPreview["oldProduct"]) {
  const code = product.code ? `${product.code} / ` : "";
  return `${code}${product.model} ${product.name}`;
}

async function handleOpenReplace(row: Product) {
  replacingProduct.value = row;
  replacementProductId.value = null;
  replacementPreview.value = null;
  replaceDialogVisible.value = true;
  if (replacementCandidates.value.length === 0) {
    loadingReplacementCandidates.value = true;
    try {
      replacementCandidates.value = await getProducts();
    } catch (e: any) {
      if (isUnauthorized(e)) return;
      ElMessage.error("替换候选加载失败");
    } finally {
      loadingReplacementCandidates.value = false;
    }
  }
}

async function handleReplacementTargetChange() {
  if (!replacingProduct.value || !replacementProductId.value) {
    replacementPreview.value = null;
    return;
  }

  loadingReplacementPreview.value = true;
  try {
    replacementPreview.value = await getProductReplacementPreview(replacingProduct.value.id, replacementProductId.value);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    replacementPreview.value = null;
    ElMessage.error(e?.response?.data?.message || "替换预览加载失败");
  } finally {
    loadingReplacementPreview.value = false;
  }
}

async function handleConfirmReplace() {
  if (!replacingProduct.value || !replacementProductId.value || !replacementPreview.value) return;
  try {
    await ElMessageBox.confirm(
      `确定将「${formatProductLabel(replacingProduct.value)}」替换为「${formatProductLabel(replacementPreview.value.newProduct)}」吗？`,
      "确认一键替换",
      { type: "warning", confirmButtonText: "确认替换" }
    );

    replacing.value = true;
    const result = await replaceSelectionResultProduct(replacingProduct.value.id, replacementProductId.value);
    replacementPreview.value = result;
    ElMessage.success(`替换完成：影响 ${result.affectedResultCount} 条选型结果`);
    replaceDialogVisible.value = false;
    await loadProducts();
  } catch (e: any) {
    if (e === "cancel" || e === "close") return;
    if (isUnauthorized(e)) return;
    ElMessage.error(e?.response?.data?.message || "替换失败");
  } finally {
    replacing.value = false;
  }
}

async function handleDelete(row: Product) {
  try {
    await ElMessageBox.confirm(`确定要删除产品 ${row.model} 吗？`, "确认删除", { type: "warning" });
    await deleteProduct(row.id);
    ElMessage.success("产品已删除");
    await loadProducts();
  } catch (e: any) {
    if (e === "cancel" || e === "close") return;
    if (isUnauthorized(e)) return;
    ElMessage.error(e?.response?.data?.message || "删除失败");
  }
}

async function handleSave() {
  if (!form.value.code?.trim() || !form.value.model || !form.value.name || !form.value.type) {
    ElMessage.warning("请填写必填字段");
    return;
  }
  try {
    if (form.value.id) {
      await updateProduct(form.value.id, form.value);
      ElMessage.success("产品已更新");
    } else {
      await createProduct(form.value);
      ElMessage.success("产品已添加");
    }
    dialogVisible.value = false;
    await loadProducts();
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
    ElMessage.error("保存失败");
  }
}

async function handleExportProducts() {
  exporting.value = true;
  try {
    const blob = await exportProducts(typeFilter.value || undefined);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    const suffix = typeFilter.value ? `-${typeFilter.value}` : "";
    link.href = url;
    link.download = `产品清单${suffix}.xlsx`;
    link.click();
    URL.revokeObjectURL(url);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
    ElMessage.error("导出失败");
  } finally {
    exporting.value = false;
  }
}

function handlePickImportFile() {
  importInputRef.value?.click();
}

async function handleImportProducts(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = "";
  if (!file) return;

  if (!file.name.toLowerCase().endsWith(".xlsx")) {
    ElMessage.warning("请选择 xlsx 文件");
    return;
  }

  importing.value = true;
  try {
    const data = new FormData();
    data.append("file", file);
    const result = await importProducts(data);
    ElMessage.success(`导入完成：新增 ${result.created} 条，更新 ${result.updated} 条`);
    await loadProducts();
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
    const errors = e?.response?.data?.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      ElMessage.error(errors.slice(0, 3).join("；"));
    } else {
      ElMessage.error(e?.response?.data?.message || "导入失败");
    }
  } finally {
    importing.value = false;
  }
}

// 类型维护操作
function handleAddType() {
  typeDialogTitle.value = "添加类型";
  typeForm.value = { id: "", name: "" };
  typeDialogVisible.value = true;
}

function handleEditType(row: SensorType) {
  typeDialogTitle.value = "编辑类型";
  typeForm.value = { id: row.id, name: row.name, _editing: true };
  typeDialogVisible.value = true;
}

async function handleDeleteType(row: SensorType) {
  try {
    await ElMessageBox.confirm(`确定要删除类型「${row.name}」吗？`, "确认删除", { type: "warning" });
    await deleteSensorType(row.id);
    ElMessage.success("类型已删除");
    await Promise.all([loadSensorTypes(), loadTypeOptions(), loadProducts()]);
  } catch (e: any) {
    if (e?.response?.status === 409) {
      ElMessage.error("该类型下还有产品，无法删除");
    }
  }
}

async function handleSaveType() {
  if (!typeForm.value.name) {
    ElMessage.warning("请填写类型名称");
    return;
  }
  try {
    if (typeForm.value._editing) {
      await updateSensorType(typeForm.value.id, { id: typeForm.value.id, name: typeForm.value.name });
      ElMessage.success("类型已更新");
    } else {
      await createSensorType({ id: "", name: typeForm.value.name });
      ElMessage.success("类型已添加");
    }
    typeDialogVisible.value = false;
    await Promise.all([loadSensorTypes(), loadTypeOptions(), loadProducts()]);
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
    ElMessage.error("保存失败，类型名称可能已存在");
  }
}

// 制程注意事项操作
function handleAddNote() {
  noteDialogTitle.value = "添加制程注意事项";
  noteForm.value = { id: 0, processName: "", characteristic: "", selectionNote: "" };
  noteDialogVisible.value = true;
}

function handleEditNote(row: ProcessNote) {
  noteDialogTitle.value = "编辑制程注意事项";
  noteForm.value = { ...row };
  noteDialogVisible.value = true;
}

async function handleDeleteNote(row: ProcessNote) {
  try {
    await ElMessageBox.confirm(`确定要删除制程「${row.processName}」的注意事项吗？`, "确认删除", { type: "warning" });
    await deleteProcessNote(row.id);
    ElMessage.success("制程注意事项已删除");
    await loadProcessNotes();
  } catch (e: any) {
    if (isUnauthorized(e)) return;
  }
}

async function handleSaveNote() {
  if (!noteForm.value.processName) {
    ElMessage.warning("请填写制程名");
    return;
  }
  try {
    if (noteForm.value.id) {
      await updateProcessNote(noteForm.value.id, noteForm.value);
      ElMessage.success("制程注意事项已更新");
    } else {
      await createProcessNote(noteForm.value);
      ElMessage.success("制程注意事项已添加");
    }
    noteDialogVisible.value = false;
    await loadProcessNotes();
  } catch (e: any) {
    if (isUnauthorized(e)) return;
    console.error(e);
    ElMessage.error(e?.response?.data?.message || "保存失败");
  }
}
</script>

<template>
  <div class="sensor-console sensor-products-page">
    <el-tabs v-model="activeTab">
      <!-- 产品列表 Tab -->
      <el-tab-pane label="产品列表" name="products">
        <div class="sensor-toolbar">
          <div class="sensor-toolbar-left">
            <el-select v-model="typeFilter" placeholder="按类型筛选" clearable style="width: 200px">
              <el-option v-for="t in typeOptions" :key="t.id" :label="t.name" :value="t.id" />
            </el-select>
            <el-input
              v-model="productKeyword"
              clearable
              placeholder="搜索料号 / 型号 / 名称 / 品牌 / 类型 / 规格 / 场景"
              class="sensor-search-input"
              @clear="loadProducts"
              @keyup.enter="loadProducts"
            >
              <template #prefix>
                <SearchLine />
              </template>
            </el-input>
            <el-button @click="loadProducts"><SearchLine />查询</el-button>
          </div>
          <div class="sensor-toolbar-right">
            <input ref="importInputRef" type="file" accept=".xlsx" class="hidden-file-input" @change="handleImportProducts" />
            <el-button :loading="importing" @click="handlePickImportFile"><Upload2Line />导入 xlsx</el-button>
            <el-button :loading="exporting" @click="handleExportProducts"><Download2Line />导出 xlsx</el-button>
            <el-button type="primary" @click="handleAdd"><AddLine />添加产品</el-button>
          </div>
        </div>

        <div class="sensor-table-wrap">
          <el-table :data="filteredProducts" border stripe height="100%" style="width: 100%">
            <el-table-column prop="model" label="型号" width="200">
              <template #default="{ row }">
                <span class="font-mono font-bold">
                  <span
                    v-for="(part, index) in splitHighlightText(row.model, productKeyword)"
                    :key="`${row.id}-model-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="code" label="料号" width="110">
              <template #default="{ row }">
                <span class="font-mono">
                  <span
                    v-for="(part, index) in splitHighlightText(row.code, productKeyword)"
                    :key="`${row.id}-code-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </span>
              </template>
            </el-table-column>
            <el-table-column label="名称" width="150">
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.name, productKeyword)"
                  :key="`${row.id}-name-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="品牌" width="100">
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.brand, productKeyword)"
                  :key="`${row.id}-brand-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="类型" width="150">
              <template #default="{ row }">
                <el-tag :type="getTypeBadge(row.type)" size="small">
                  <span
                    v-for="(part, index) in splitHighlightText(getTypeName(row.type), productKeyword)"
                    :key="`${row.id}-type-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="规格" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.spec || '-', productKeyword)"
                  :key="`${row.id}-spec-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="场景" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.scene || '-', productKeyword)"
                  :key="`${row.id}-scene-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="210" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="primary" link @click="handleEdit(row)"><EditLine />编辑</el-button>
                <el-button size="small" type="warning" link @click="handleOpenReplace(row)"><ExchangeLine />替换</el-button>
                <el-button size="small" type="danger" link @click="handleDelete(row)"><DeleteBinLine />删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-tab-pane>

      <!-- 类型维护 Tab -->
      <el-tab-pane label="类型维护" name="types">
        <div class="sensor-toolbar">
          <div class="sensor-toolbar-left">
            <el-input
              v-model="typeKeyword"
              clearable
              placeholder="搜索类型名称"
              class="sensor-search-input"
              @clear="loadSensorTypes"
              @keyup.enter="loadSensorTypes"
            >
              <template #prefix>
                <SearchLine />
              </template>
            </el-input>
            <el-button @click="loadSensorTypes"><SearchLine />查询</el-button>
          </div>
          <div class="sensor-toolbar-right">
            <el-button type="primary" size="small" @click="handleAddType"><AddLine />添加类型</el-button>
          </div>
        </div>
        <div class="sensor-table-wrap">
          <el-table :data="sensorTypes" border stripe height="100%">
            <el-table-column label="类型名称" min-width="200">
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.name, typeKeyword)"
                  :key="`${row.id}-type-name-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150">
              <template #default="{ row }">
                <el-button size="small" type="primary" link @click="handleEditType(row)"><EditLine />编辑</el-button>
                <el-button size="small" type="danger" link @click="handleDeleteType(row)"><DeleteBinLine />删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-tab-pane>

      <!-- 制程注意事项 Tab -->
      <el-tab-pane label="制程注意事项" name="processNotes">
        <div class="sensor-toolbar">
          <div class="sensor-toolbar-left">
            <el-input
              v-model="processNoteKeyword"
              clearable
              placeholder="搜索制程名 / 特性 / 选型注意事项"
              class="sensor-search-input"
              @clear="loadProcessNotes"
              @keyup.enter="loadProcessNotes"
            >
              <template #prefix>
                <SearchLine />
              </template>
            </el-input>
            <el-button @click="loadProcessNotes"><SearchLine />查询</el-button>
          </div>
          <div class="sensor-toolbar-right">
            <el-button type="primary" size="small" @click="handleAddNote"><AddLine />添加注意事项</el-button>
          </div>
        </div>
        <div class="sensor-table-wrap">
          <el-table :data="processNotes" border stripe height="100%" style="width: 100%">
            <el-table-column label="制程名" width="200">
              <template #default="{ row }">
                <span
                  v-for="(part, index) in splitHighlightText(row.processName, processNoteKeyword)"
                  :key="`${row.id}-process-name-${index}`"
                  :class="{ 'keyword-highlight': part.matched }"
                >
                  {{ part.text }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="特性" min-width="260">
              <template #default="{ row }">
                <div class="pre-line-cell">
                  <span
                    v-for="(part, index) in splitHighlightText(row.characteristic || '-', processNoteKeyword)"
                    :key="`${row.id}-characteristic-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </div>
              </template>
            </el-table-column>
            <el-table-column label="选型注意事项" min-width="360">
              <template #default="{ row }">
                <div class="pre-line-cell">
                  <span
                    v-for="(part, index) in splitHighlightText(row.selectionNote || '-', processNoteKeyword)"
                    :key="`${row.id}-selection-note-${index}`"
                    :class="{ 'keyword-highlight': part.matched }"
                  >
                    {{ part.text }}
                  </span>
                </div>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="primary" link @click="handleEditNote(row)"><EditLine />编辑</el-button>
                <el-button size="small" type="danger" link @click="handleDeleteNote(row)"><DeleteBinLine />删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-tab-pane>
    </el-tabs>

    <!-- 产品编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="min(500px, calc(100vw - 24px))">
      <el-form :model="form" label-width="80px" class="dialog-form">
        <el-form-item label="型号" required>
          <el-input v-model="form.model" placeholder="如 FAB-18D16N1-D3" />
        </el-form-item>
        <el-form-item label="料号" required>
          <el-input v-model="form.code" placeholder="如 p1" />
        </el-form-item>
        <el-form-item label="名称" required>
          <el-input v-model="form.name" placeholder="如 标准近接开关" />
        </el-form-item>
        <el-form-item label="品牌">
          <el-input v-model="form.brand" />
        </el-form-item>
        <el-form-item label="类型" required>
          <el-select v-model="form.type" placeholder="选择传感器类型" style="width: 100%">
            <el-option v-for="t in typeOptions" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="规格">
          <el-input v-model="form.spec" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="场景">
          <el-input v-model="form.scene" placeholder="手动填写适用场景" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave"><Save3Line />保存</el-button>
      </template>
    </el-dialog>

    <!-- 一键替换感应器 -->
    <el-dialog
      v-model="replaceDialogVisible"
      title="一键替换感应器"
      width="min(640px, calc(100vw - 24px))"
    >
      <div v-if="replacingProduct" class="replacement-dialog">
        <div class="replacement-current">
          <div class="replacement-label">旧感应器</div>
          <div class="replacement-product">{{ formatProductLabel(replacingProduct) }}</div>
        </div>

        <el-form label-width="96px">
          <el-form-item label="替换为" required>
            <el-select
              v-model="replacementProductId"
              filterable
              :loading="loadingReplacementCandidates"
              placeholder="选择新的感应器"
              style="width: 100%"
              @change="handleReplacementTargetChange"
            >
              <el-option
                v-for="product in replacementOptions"
                :key="product.id"
                :label="formatProductLabel(product)"
                :value="product.id"
              />
            </el-select>
          </el-form-item>
        </el-form>

        <div v-loading="loadingReplacementPreview" class="replacement-preview">
          <template v-if="replacementPreview">
            <div class="replacement-stats">
              <div>
                <span>选型结果</span>
                <strong>{{ replacementPreview.affectedResultCount }}</strong>
              </div>
              <div>
                <span>涉及分类</span>
                <strong>{{ replacementPreview.affectedNodeCount }}</strong>
              </div>
              <div>
                <span>合并数量</span>
                <strong>{{ replacementPreview.mergeResultCount }}</strong>
              </div>
            </div>

            <div v-if="replacementResultGroups.length" class="replacement-detail-list">
              <div
                v-for="group in replacementResultGroups"
                :key="group.nodeId"
                class="replacement-node-group"
              >
                <div class="replacement-node-title">
                  <span>{{ group.nodeName }}</span>
                  <el-tag size="small" type="info">{{ group.results.length }} 条选型结果</el-tag>
                </div>
                <div
                  v-for="result in group.results"
                  :key="result.resultId"
                  class="replacement-result-item"
                >
                  <div class="replacement-result-note">{{ result.note || "未填写作用" }}</div>
                  <div class="replacement-result-meta">
                    <span>旧数量 {{ result.oldQuantity }}</span>
                    <span v-if="result.willMerge">已有新感应器 {{ result.existingNewQuantity }}</span>
                    <span>替换后 {{ result.finalNewQuantity }}</span>
                    <el-tag v-if="result.willMerge" size="small" type="warning">合并</el-tag>
                    <el-tag v-else size="small" type="success">替换</el-tag>
                  </div>
                </div>
              </div>
            </div>
            <el-empty v-else description="当前旧感应器没有被选型结果引用" :image-size="60" />
          </template>
          <el-empty v-else description="请选择新的感应器查看影响范围" :image-size="60" />
        </div>
      </div>
      <template #footer>
        <el-button @click="replaceDialogVisible = false">取消</el-button>
        <el-button
          type="warning"
          :loading="replacing"
          :disabled="!replacementPreview || loadingReplacementPreview"
          @click="handleConfirmReplace"
        >
          <ExchangeLine />确认替换
        </el-button>
      </template>
    </el-dialog>

    <!-- 制程注意事项编辑对话框 -->
    <el-dialog v-model="noteDialogVisible" :title="noteDialogTitle" width="min(560px, calc(100vw - 24px))">
      <el-form :model="noteForm" label-width="110px">
        <el-form-item label="制程名" required>
          <el-input v-model="noteForm.processName" placeholder="如 电镀制程" />
        </el-form-item>
        <el-form-item label="特性">
          <el-input v-model="noteForm.characteristic" type="textarea" :rows="3" placeholder="填写该制程的特性" />
        </el-form-item>
        <el-form-item label="选型注意事项">
          <el-input v-model="noteForm.selectionNote" type="textarea" :rows="4" placeholder="填写选型注意事项" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="noteDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveNote"><Save3Line />保存</el-button>
      </template>
    </el-dialog>

    <!-- 类型编辑对话框 -->
    <el-dialog v-model="typeDialogVisible" :title="typeDialogTitle" width="min(400px, calc(100vw - 24px))">
      <el-form :model="typeForm" label-width="80px">
        <el-form-item label="类型名称" required>
          <el-input v-model="typeForm.name" placeholder="如 近接开关(φ18)" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveType"><Save3Line />保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
@use "./shared-ui.scss";

.dialog-form :deep(.el-form-item) {
  margin-bottom: 24px;
}

.dialog-form :deep(.el-form-item:last-child) {
  margin-bottom: 0;
}

@media (max-width: 900px) {
  .sensor-products-page :deep(.el-select) {
    width: 100% !important;
  }
}

.hidden-file-input {
  display: none;
}

.sensor-search-input {
  width: min(420px, 42vw);
}

.keyword-highlight {
  padding: 0 2px;
  color: #92400e;
  background: #fef3c7;
  border-radius: 3px;
}

.pre-line-cell {
  line-height: 1.7;
  white-space: pre-line;
}

.replacement-dialog {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.replacement-current {
  padding: 12px;
  background: var(--sensor-color-bg);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
}

.replacement-label {
  margin-bottom: 4px;
  font-size: 12px;
  font-weight: 700;
  color: var(--sensor-color-muted);
}

.replacement-product {
  font-size: 14px;
  font-weight: 800;
  color: var(--sensor-color-text);
  overflow-wrap: anywhere;
}

.replacement-preview {
  min-height: 150px;
}

.replacement-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}

.replacement-stats > div {
  display: flex;
  min-height: 66px;
  flex-direction: column;
  justify-content: center;
  padding: 10px 12px;
  background: var(--sensor-color-bg);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-md);
}

.replacement-stats span {
  font-size: 12px;
  color: var(--sensor-color-muted);
}

.replacement-stats strong {
  margin-top: 4px;
  font-size: 22px;
  color: var(--sensor-color-text);
}

.replacement-detail-list {
  display: flex;
  max-height: 300px;
  flex-direction: column;
  gap: 10px;
  overflow: auto;
  padding-right: 4px;
}

.replacement-node-group {
  overflow: hidden;
  background: var(--sensor-color-surface);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-sm);
}

.replacement-node-title {
  display: flex;
  gap: 10px;
  align-items: center;
  justify-content: space-between;
  padding: 9px 11px;
  font-weight: 800;
  background: var(--sensor-color-bg);
  border-bottom: 1px solid var(--sensor-color-border);
}

.replacement-node-title span {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.replacement-result-item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 10px;
  align-items: center;
  padding: 10px 11px;
}

.replacement-result-item + .replacement-result-item {
  border-top: 1px solid var(--sensor-color-border);
}

.replacement-result-note {
  min-width: 0;
  font-size: 14px;
  font-weight: 700;
  line-height: 1.6;
  color: var(--sensor-color-text);
  white-space: pre-line;
  overflow-wrap: anywhere;
}

.replacement-result-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
  justify-content: flex-end;
  max-width: 260px;
  font-size: 12px;
  color: var(--sensor-color-muted);
}

.replacement-result-meta span {
  padding: 2px 6px;
  white-space: nowrap;
  background: var(--sensor-color-bg);
  border: 1px solid var(--sensor-color-border);
  border-radius: var(--sensor-radius-sm);
}

@media (max-width: 640px) {
  .replacement-stats {
    grid-template-columns: 1fr;
  }

  .replacement-result-item {
    grid-template-columns: 1fr;
  }

  .replacement-result-meta {
    justify-content: flex-start;
    max-width: none;
  }
}
</style>

<template>
  <PageShell>
    <template #toolbar>
      <div class="toolbar-title">
        <strong>{{ title }}</strong>
        <span>{{ filteredRows.length }} 条记录</span>
      </div>
      <div class="toolbar-actions">
        <el-input
          v-model="keyword"
          clearable
          placeholder="搜索型号"
          :prefix-icon="Search"
          class="search-input"
        />
        <el-button type="primary" :icon="Plus" @click="startCreate">新增</el-button>
        <el-button :icon="Refresh" @click="load">刷新</el-button>
      </div>
    </template>

    <el-table
      v-loading="loading"
      :data="filteredRows"
      border
      stripe
      height="100%"
      class="admin-table"
    >
      <el-table-column
        v-for="column in columns"
        :key="column.prop"
        :prop="column.prop"
        :label="column.label"
        :width="column.width"
        :min-width="column.minWidth"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          {{ column.format ? column.format(row[column.prop], row) : row[column.prop] }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="150" fixed="right" align="center">
        <template #default="{ row }">
          <el-button link type="primary" @click="startEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑记录' : '新增记录'" width="760px">
      <el-form :model="form" label-width="120px" class="edit-grid">
        <el-form-item v-for="column in columns" :key="column.prop" :label="column.label">
          <el-input
            v-if="column.type === 'text'"
            v-model="form[column.prop]"
            clearable
          />
          <el-input-number
            v-else
            v-model="form[column.prop]"
            :precision="column.precision ?? 0"
            :step="column.step ?? 1"
            :disabled="column.readonly"
            controls-position="right"
            class="number-input"
            @update:model-value="applyCalculatedFields"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh, Search } from '@element-plus/icons-vue';
import PageShell from './PageShell.vue';
import type { CatalogColumn } from '../types/catalogTable';
import { applyCalculatedCatalogFields } from '../utils/catalogForm';

const props = defineProps<{
  title: string;
  columns: CatalogColumn[];
  getRows: () => Promise<object[]>;
  createRow: (row: Record<string, unknown>) => Promise<unknown>;
  updateRow: (id: number, row: Record<string, unknown>) => Promise<unknown>;
  deleteRow: (id: number) => Promise<unknown>;
}>();

const rows = ref<Record<string, unknown>[]>([]);
const loading = ref(false);
const keyword = ref('');
const dialogVisible = ref(false);
const editingId = ref<number | null>(null);
const form = reactive<Record<string, unknown>>({});

const filteredRows = computed(() => {
  const q = keyword.value.trim().toLowerCase();
  if (!q) return rows.value;
  return rows.value.filter(row => JSON.stringify(row).toLowerCase().includes(q));
});

function emptyForm() {
  props.columns.forEach(column => {
    form[column.prop] = column.defaultValue ?? (column.type === 'text' ? '' : 0);
  });
  applyCalculatedFields();
}

async function load() {
  loading.value = true;
  try {
    rows.value = (await props.getRows()) as Record<string, unknown>[];
  } finally {
    loading.value = false;
  }
}

function startCreate() {
  editingId.value = null;
  emptyForm();
  dialogVisible.value = true;
}

function startEdit(row: Record<string, unknown>) {
  editingId.value = Number(row.id);
  props.columns.forEach(column => {
    form[column.prop] = row[column.prop] ?? (column.type === 'text' ? '' : 0);
  });
  applyCalculatedFields();
  dialogVisible.value = true;
}

function applyCalculatedFields() {
  applyCalculatedCatalogFields(props.columns, form);
}

async function save() {
  applyCalculatedFields();
  const payload = { ...form };
  if (editingId.value) {
    await props.updateRow(editingId.value, payload);
  } else {
    await props.createRow(payload);
  }
  ElMessage.success('已保存');
  dialogVisible.value = false;
  await load();
}

async function remove(row: Record<string, unknown>) {
  await ElMessageBox.confirm('确认删除这条记录？', '删除确认', { type: 'warning' });
  await props.deleteRow(Number(row.id));
  ElMessage.success('已删除');
  await load();
}

onMounted(load);
</script>

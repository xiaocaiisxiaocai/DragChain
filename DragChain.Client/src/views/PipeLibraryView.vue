<template>
  <PageShell>
    <template #toolbar>
      <div class="toolbar-title">
        <strong>管线库</strong>
        <span>{{ filteredRows.length }} 条记录</span>
      </div>
      <div class="toolbar-actions">
        <el-input v-model="keyword" clearable placeholder="搜索管线" :prefix-icon="Search" class="search-input" />
        <el-button type="primary" :icon="Plus" @click="startCreate">新增</el-button>
        <el-button :icon="Refresh" @click="load">刷新</el-button>
      </div>
    </template>

    <el-table v-loading="loading" :data="filteredRows" border stripe height="100%" class="admin-table">
      <el-table-column prop="name" label="管线名称" min-width="180" show-overflow-tooltip />
      <el-table-column prop="type" label="分类" width="120">
        <template #default="{ row }">
          <el-tag>{{ getPipeDisplayLabel(row) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="diameter" label="直径 mm" width="110" />
      <el-table-column prop="weight" label="重量 kg/m" width="130" />
      <el-table-column prop="bendMultiplier" label="弯曲倍数" width="120" />
      <el-table-column label="操作" width="150" fixed="right" align="center">
        <template #default="{ row }">
          <el-button link type="primary" @click="startEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row.id)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑管线' : '新增管线'" width="560px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="管线名称"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="分类">
          <el-select v-model="form.type" class="full-input">
            <el-option label="气管" value="tube" />
            <el-option label="弱电电缆" value="weak_cable" />
            <el-option label="强电电缆" value="strong_cable" />
            <el-option label="编码器线" value="encoder" />
            <el-option label="其他" value="other" />
          </el-select>
        </el-form-item>
        <el-form-item label="直径 mm"><el-input-number v-model="form.diameter" :min="0" :step="0.1" controls-position="right" /></el-form-item>
        <el-form-item label="重量 kg/m"><el-input-number v-model="form.weight" :min="0" :step="0.001" :precision="3" controls-position="right" /></el-form-item>
        <el-form-item label="弯曲倍数"><el-input-number v-model="form.bendMultiplier" :min="0" :step="1" controls-position="right" /></el-form-item>
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
import PageShell from '../components/PageShell.vue';
import { pipeLibraryApi, type CreatePipeType } from '../api/pipeLibrary';
import type { PipeType } from '../types';
import { getPipeDisplayLabel, getPipeDisplayType, toBackendPipeType } from '../utils/pipeType';

const rows = ref<PipeType[]>([]);
const loading = ref(false);
const keyword = ref('');
const dialogVisible = ref(false);
const editingId = ref<number | null>(null);
const form = reactive<CreatePipeType>({
  name: '',
  type: 'tube',
  diameter: 0,
  weight: 0,
  bendMultiplier: 10
});

const filteredRows = computed(() => {
  const q = keyword.value.trim().toLowerCase();
  if (!q) return rows.value;
  return rows.value.filter(row => JSON.stringify(row).toLowerCase().includes(q));
});

async function load() {
  loading.value = true;
  try {
    rows.value = await pipeLibraryApi.getAll();
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.name = '';
  form.type = 'tube';
  form.diameter = 0;
  form.weight = 0;
  form.bendMultiplier = 10;
}

function startCreate() {
  editingId.value = null;
  resetForm();
  dialogVisible.value = true;
}

function startEdit(row: PipeType) {
  editingId.value = row.id;
  form.name = row.name;
  form.type = getPipeDisplayType(row);
  form.diameter = row.diameter;
  form.weight = row.weight;
  form.bendMultiplier = row.bendMultiplier;
  dialogVisible.value = true;
}

async function save() {
  const payload = { ...form, type: toBackendPipeType(form.type) };
  if (editingId.value) {
    await pipeLibraryApi.update(editingId.value, payload);
  } else {
    await pipeLibraryApi.create(payload);
  }
  ElMessage.success('已保存');
  dialogVisible.value = false;
  await load();
}

async function remove(id: number) {
  await ElMessageBox.confirm('确认删除这条管线？', '删除确认', { type: 'warning' });
  await pipeLibraryApi.delete(id);
  ElMessage.success('已删除');
  await load();
}

onMounted(load);
</script>

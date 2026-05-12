<template>
  <PageShell>
    <template #toolbar>
      <div class="toolbar-title">
        <strong>元件库</strong>
        <span>{{ filteredRows.length }} 个元件</span>
      </div>
      <div class="toolbar-actions">
        <el-input v-model="keyword" clearable placeholder="搜索元件或管线" :prefix-icon="Search" class="search-input" />
        <el-button type="primary" :icon="Plus" @click="startCreate">新增元件</el-button>
        <el-button :icon="Refresh" @click="load">刷新</el-button>
      </div>
    </template>

    <el-table v-loading="loading" :data="filteredRows" border stripe height="100%" class="admin-table">
      <el-table-column type="expand" width="48">
        <template #default="{ row }">
          <el-table :data="row.items" size="small" border class="module-inner-table">
            <el-table-column label="管线名称" min-width="180">
              <template #default="{ row: item }">
                {{ item.pipeType?.name || `#${item.pipeTypeId}` }}
              </template>
            </el-table-column>
            <el-table-column label="分类" width="110">
              <template #default="{ row: item }">
                <el-tag v-if="item.pipeType" size="small">{{ getPipeDisplayLabel(item.pipeType) }}</el-tag>
                <span v-else>-</span>
              </template>
            </el-table-column>
            <el-table-column label="直径 mm" width="100">
              <template #default="{ row: item }">{{ item.pipeType?.diameter ?? '-' }}</template>
            </el-table-column>
            <el-table-column label="重量 kg/m" width="120">
              <template #default="{ row: item }">{{ item.pipeType?.weight ?? '-' }}</template>
            </el-table-column>
            <el-table-column prop="qty" label="元件内数量" width="110" />
          </el-table>
        </template>
      </el-table-column>
      <el-table-column prop="name" label="元件名称" min-width="180" show-overflow-tooltip />
      <el-table-column prop="description" label="说明" min-width="180" show-overflow-tooltip />
      <el-table-column label="包含内容" min-width="260" show-overflow-tooltip>
        <template #default="{ row }">{{ describeComponent(row) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="150" fixed="right" align="center">
        <template #default="{ row }">
          <el-button link type="primary" @click="startEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row.id)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑元件' : '新增元件'" width="760px">
      <el-form :model="form" label-width="90px">
        <el-form-item label="元件名称"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="说明"><el-input v-model="form.description" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="包含管线">
          <div class="module-editor">
            <div v-for="(item, index) in form.items" :key="index" class="module-editor-row">
              <el-select v-model="item.pipeTypeId" filterable class="module-pipe-select" placeholder="选择管线">
                <el-option
                  v-for="pipe in pipeLib"
                  :key="pipe.id"
                  :label="`${pipe.name} · ${getPipeDisplayLabel(pipe)} · Φ${pipe.diameter}`"
                  :value="pipe.id"
                />
              </el-select>
              <el-input-number v-model="item.qty" :min="1" :step="1" controls-position="right" />
              <el-button link type="danger" @click="removeItem(index)">删除</el-button>
            </div>
            <el-button :icon="Plus" @click="addItem">添加管线</el-button>
          </div>
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
import PageShell from '../components/PageShell.vue';
import { pipeComponentsApi, type CreatePipeComponent } from '../api/pipeComponents';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import type { PipeComponent } from '../types';
import { getPipeDisplayLabel } from '../utils/pipeType';

const rows = ref<PipeComponent[]>([]);
const loading = ref(false);
const keyword = ref('');
const dialogVisible = ref(false);
const editingId = ref<number | null>(null);
const { pipeLib, loadPipeLib } = usePipeLibrary();

const form = reactive<CreatePipeComponent>({
  name: '',
  description: '',
  items: []
});

const filteredRows = computed(() => {
  const q = keyword.value.trim().toLowerCase();
  if (!q) return rows.value;
  return rows.value.filter(row => `${row.name} ${row.description} ${describeComponent(row)}`.toLowerCase().includes(q));
});

async function load() {
  loading.value = true;
  try {
    rows.value = await pipeComponentsApi.getAll();
  } finally {
    loading.value = false;
  }
}

function describeComponent(component: PipeComponent) {
  return component.items
    .map(item => `${item.pipeType?.name || `#${item.pipeTypeId}`}×${item.qty}`)
    .join('，');
}

function resetForm() {
  form.name = '';
  form.description = '';
  form.items = [{ pipeTypeId: pipeLib.value[0]?.id || 0, qty: 1 }];
}

function startCreate() {
  editingId.value = null;
  resetForm();
  dialogVisible.value = true;
}

function startEdit(row: PipeComponent) {
  editingId.value = row.id;
  form.name = row.name;
  form.description = row.description;
  form.items = row.items.map(item => ({ pipeTypeId: item.pipeTypeId, qty: item.qty }));
  dialogVisible.value = true;
}

function addItem() {
  form.items.push({ pipeTypeId: pipeLib.value[0]?.id || 0, qty: 1 });
}

function removeItem(index: number) {
  form.items.splice(index, 1);
}

async function save() {
  const payload = {
    name: form.name.trim(),
    description: form.description.trim(),
    items: form.items.filter(item => item.pipeTypeId > 0 && item.qty > 0)
  };

  if (!payload.name) {
    ElMessage.warning('请填写元件名称');
    return;
  }
  if (!payload.items.length) {
    ElMessage.warning('元件至少需要包含一条管线');
    return;
  }

  if (editingId.value) {
    await pipeComponentsApi.update(editingId.value, payload);
  } else {
    await pipeComponentsApi.create(payload);
  }
  ElMessage.success('已保存');
  dialogVisible.value = false;
  await load();
}

async function remove(id: number) {
  await ElMessageBox.confirm('确认删除这个元件？', '删除确认', { type: 'warning' });
  await pipeComponentsApi.delete(id);
  ElMessage.success('已删除');
  await load();
}

onMounted(async () => {
  await Promise.all([loadPipeLib(), load()]);
});
</script>

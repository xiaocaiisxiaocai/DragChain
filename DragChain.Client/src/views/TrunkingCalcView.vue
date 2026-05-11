<template>
  <PageShell>
    <div class="split-workspace">
      <aside class="work-panel">
        <el-card shadow="never" class="control-card">
          <template #header>选型参数</template>
          <div class="form-line">
            <span>填充率上限</span>
            <el-input-number
              v-model="fillRatio"
              :min="1"
              :max="100"
              :step="1"
              :disabled="settingsLoading"
              controls-position="right"
            />
            <span>%</span>
            <el-text v-if="settingsSaving" size="small" type="info">保存中</el-text>
            <el-text v-else-if="settingsSaved" size="small" type="success">已保存</el-text>
          </div>
        </el-card>

        <el-card shadow="never" class="control-card pipe-list-card">
          <template #header>
            <div class="card-header-row">
              <span>管线清单</span>
              <el-button size="small" type="primary" @click="showAddDialog = true">新增管线</el-button>
            </div>
          </template>
          <el-table
            :data="enrichedPipes"
            row-key="selectionKey"
            :tree-props="{ children: 'children' }"
            :row-class-name="selectionRowClass"
            size="small"
            border
            height="100%"
            class="selection-table"
            >
            <el-table-column prop="name" label="管线 / 模块" min-width="150" show-overflow-tooltip>
              <template #default="{ row }">
                <div class="selection-name">
                  <el-tag v-if="row.kind === 'module'" size="small" type="warning">模块</el-tag>
                  <el-tag v-else-if="row.typeLabel === '强电电缆'" size="small" type="danger">强电</el-tag>
                  <el-tag v-else-if="row.typeLabel === '弱电电缆'" size="small" type="primary">弱电</el-tag>
                  <el-tag v-else-if="row.typeLabel === '编码器'" size="small" type="warning">编码器</el-tag>
                  <el-tag v-else-if="row.typeLabel === '气管'" size="small" type="success">气管</el-tag>
                  <span>{{ row.name }}</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column label="数量" width="72" align="center">
              <template #default="{ row }">
                <el-input-number
                  v-if="row.kind !== 'module-item'"
                  class="pipe-qty-input"
                  :model-value="row.qty"
                  :min="0"
                  :step="1"
                  size="small"
                  controls-position="right"
                  @update:model-value="(value: number | undefined) => updateQty(row.sourceIndex, Number(value || 0))"
                />
                <span v-else>{{ row.qty }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="sizeText" label="尺寸" width="92" />
            <el-table-column prop="areaText" label="面积" width="78" align="right" />
            <el-table-column width="52" align="center">
              <template #default="{ row }">
                <el-button v-if="row.kind !== 'module-item'" link type="danger" @click="removePipe(row.sourceIndex)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </aside>

      <section class="result-area">
        <el-card shadow="never" class="result-card">
          <template #header>
            <div class="card-header-row">
              <span>填充率核算</span>
              <el-tag type="primary">线槽容纳判定</el-tag>
            </div>
          </template>
          <el-skeleton v-if="loading" :rows="4" animated />
          <el-alert v-else-if="error" :title="error" type="error" show-icon :closable="false" />
          <template v-else>
            <div class="ratio-row">
              <span>实际填充率</span>
              <strong :class="displayResultStatus === 'ok' ? 'ok-text' : 'danger-text'">
                {{ hasTrunkingPipes && result ? (result.actualFillRatio * 100).toFixed(1) : '0.0' }}%
              </strong>
            </div>
            <el-progress
              :percentage="hasTrunkingPipes ? Math.min((result?.actualFillRatio || 0) * 100, 100) : 0"
              :status="displayResultStatus === 'ok' ? 'success' : 'exception'"
              :stroke-width="18"
            />
            <div class="conclusion-box" :class="displayResultStatus">
              <strong>{{ displayResultMessage }}</strong>
              <span v-if="hasTrunkingPipes && result?.weakSide?.selectedTrunking">左侧弱电 {{ result.weakSide.selectedTrunking.model }} · 面积 {{ result.weakSide.selectedTrunking.crossSection }} mm²</span>
              <span v-if="hasTrunkingPipes && result?.strongSide?.selectedTrunking">右侧强电 {{ result.strongSide.selectedTrunking.model }} · 面积 {{ result.strongSide.selectedTrunking.crossSection }} mm²</span>
            </div>
          </template>
        </el-card>

        <div class="trunking-side-grid">
          <el-card v-for="side in trunkingSides" :key="side.key" shadow="never" class="result-card">
            <template #header>
              <div class="card-header-row">
                <span>{{ side.title }}</span>
                <el-tag :type="side.result?.resultStatus === 'err' ? 'danger' : 'success'" effect="plain">
                  {{ side.result?.resultMessage || '无数据' }}
                </el-tag>
              </div>
            </template>
            <div class="side-summary">
              <MetricItem label="管线面积" :value="side.result?.totalArea?.toFixed(0)" unit="mm²" />
              <MetricItem label="推荐线槽" :value="side.result?.selectedTrunking?.model || '-'" />
              <MetricItem label="填充率" :value="side.result ? (side.result.actualFillRatio * 100).toFixed(1) : undefined" unit="%" />
              <MetricItem label="线槽面积" :value="side.result?.selectedTrunking?.crossSection?.toFixed(0)" unit="mm²" />
            </div>
            <el-table :data="side.result?.matchResults || []" size="small" border max-height="260">
              <el-table-column prop="model" label="线槽型号" min-width="110" show-overflow-tooltip />
              <el-table-column label="尺寸" width="100">
                <template #default="{ row }">{{ row.width }}×{{ row.height }}</template>
              </el-table-column>
              <el-table-column label="面积" width="82" align="right">
                <template #default="{ row }">{{ row.crossSection.toFixed(0) }}</template>
              </el-table-column>
              <el-table-column label="填充率" width="82" align="right">
                <template #default="{ row }">{{ (row.actualFillRatio * 100).toFixed(1) }}%</template>
              </el-table-column>
              <el-table-column label="推荐" width="72" align="center">
                <template #default="{ row }">
                  <el-tag v-if="row.isRecommended" type="primary" size="small">推荐</el-tag>
                  <span v-else>-</span>
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </div>

      </section>
    </div>

    <AddPipeDialog
      v-model="showAddDialog"
      :pipe-lib="trunkingPipeLib"
      :pipe-modules="trunkingModules"
      :active-pipes="activePipes"
      :allowed-types="TRUNKING_ALLOWED_TYPES"
      @confirm="addPipes"
    />
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import AddPipeDialog from '../components/AddPipeDialog.vue';
import MetricItem from '../widgets/MetricItem.vue';
import PageShell from '../components/PageShell.vue';
import { trunkingApi } from '../api/trunking';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeModules } from '../composables/usePipeModules';
import type { ActivePipe, PipeType, TrunkingCalcResponse } from '../types';
import { expandSelectionToPipes } from '../utils/pipeSelection';
import { getPipeDisplayType } from '../utils/pipeType';
import { createTrunkingSelectionRows } from '../utils/trunkingSelectionDisplay';

const TRUNKING_ALLOWED_TYPES = ['weak_cable', 'strong_cable', 'encoder'];

const { pipeLib, loadPipeLib } = usePipeLibrary();
const { pipeModules, loadPipeModules } = usePipeModules();
const fillRatio = ref(75);
const activePipes = ref<ActivePipe[]>([]);
const result = ref<TrunkingCalcResponse | null>(null);
const loading = ref(false);
const error = ref('');
const settingsLoading = ref(false);
const settingsSaving = ref(false);
const settingsSaved = ref(false);
const showAddDialog = ref(false);

function isTrunkingPipe(pipe?: Pick<PipeType, 'name' | 'type'> | null) {
  return pipe ? TRUNKING_ALLOWED_TYPES.includes(getPipeDisplayType(pipe)) : false;
}

const trunkingPipeLib = computed(() => pipeLib.value.filter(isTrunkingPipe));
const trunkingModules = computed(() => pipeModules.value.map(module => ({
  ...module,
  items: module.items.filter(item => {
    const pipe = pipeLib.value.find(pipeItem => pipeItem.id === item.pipeTypeId) || item.pipeType;
    return isTrunkingPipe(pipe);
  })
})).filter(module => module.items.length > 0));
const trunkingActivePipes = computed(() => activePipes.value.filter(pipe => {
  if (pipe.kind === 'module') return trunkingModules.value.some(module => module.id === pipe.moduleId);
  const item = pipeLib.value.find(pipeItem => pipeItem.id === pipe.libId);
  return isTrunkingPipe(item);
}));
const enrichedPipes = computed(() => createTrunkingSelectionRows(trunkingActivePipes.value, pipeLib.value, trunkingModules.value));
const hasTrunkingPipes = computed(() => trunkingActivePipes.value.some(pipe => pipe.qty > 0));
const displayResultStatus = computed(() => (hasTrunkingPipes.value ? result.value?.resultStatus || 'warn' : 'warn'));
const displayResultMessage = computed(() => (hasTrunkingPipes.value ? result.value?.resultMessage || '请选择线槽和管线' : '请填写管线清单'));
const trunkingSides = computed(() => [
  { key: 'weak', title: '左侧弱电线槽', result: result.value?.weakSide || null },
  { key: 'strong', title: '右侧强电线槽', result: result.value?.strongSide || null }
]);

async function calculate() {
  loading.value = true;
  error.value = '';
  try {
    result.value = await trunkingApi.calculate({
      selectedTrunkingId: 0,
      fillRatio: fillRatio.value / 100,
      pipes: expandSelectionToPipes(trunkingActivePipes.value, trunkingModules.value)
    });
  } catch (err) {
    error.value = err instanceof Error ? err.message : '计算失败';
  } finally {
    loading.value = false;
  }
}

async function loadSettings() {
  settingsLoading.value = true;
  try {
    const settings = await trunkingApi.getSettings();
    fillRatio.value = Math.round(settings.fillRatio * 100);
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '读取填充率上限失败');
  } finally {
    settingsLoading.value = false;
  }
}

async function saveSettings() {
  settingsSaving.value = true;
  settingsSaved.value = false;
  try {
    await trunkingApi.updateSettings({ fillRatio: fillRatio.value / 100 });
    settingsSaved.value = true;
    window.setTimeout(() => {
      settingsSaved.value = false;
    }, 1600);
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '保存填充率上限失败');
  } finally {
    settingsSaving.value = false;
  }
}

function updateQty(index: number, qty: number) {
  activePipes.value[index] = { ...activePipes.value[index], qty };
}

function removePipe(index: number) {
  activePipes.value.splice(index, 1);
}

function selectionRowClass({ row }: { row: { kind?: string } }) {
  return row.kind === 'module-item' ? 'module-detail-row' : '';
}

function addPipes(payload: { pipeIds: number[]; moduleIds: number[] }) {
  const existingPipeIds = new Set(activePipes.value.filter(pipe => pipe.kind !== 'module').map(pipe => pipe.libId));
  const existingModuleIds = new Set(activePipes.value.filter(pipe => pipe.kind === 'module').map(pipe => pipe.moduleId));
  activePipes.value.push(
    ...payload.pipeIds
      .filter(id => !existingPipeIds.has(id))
      .map(id => ({ kind: 'pipe' as const, libId: id, qty: 1 })),
    ...payload.moduleIds
      .filter(id => !existingModuleIds.has(id))
      .map(id => ({ kind: 'module' as const, moduleId: id, qty: 1 }))
  );
}

let calculateTimer = 0;
let saveSettingsTimer = 0;

watch(activePipes, () => {
  window.clearTimeout(calculateTimer);
  calculateTimer = window.setTimeout(calculate, 300);
}, { deep: true });

watch(fillRatio, () => {
  window.clearTimeout(calculateTimer);
  window.clearTimeout(saveSettingsTimer);
  calculateTimer = window.setTimeout(calculate, 300);
  // 输入框变化频繁，防抖后再持久化到数据库。
  saveSettingsTimer = window.setTimeout(saveSettings, 600);
});

onMounted(async () => {
  await Promise.all([loadSettings(), loadPipeLib(), loadPipeModules()]);
  await calculate();
});
</script>

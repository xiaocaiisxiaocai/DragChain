<template>
  <PageShell>
    <div class="split-workspace">
      <aside class="work-panel">
        <el-card shadow="never" class="control-card">
          <template #header>拖链品牌</template>
          <el-select v-model="brand" class="full-input" @change="handleBrandChange">
            <el-option label="沃德无尘拖链 WZL" value="wzl" />
            <el-option label="犸幕普通拖链 ME" value="me" />
          </el-select>
        </el-card>

        <el-card shadow="never" class="control-card">
          <template #header>感应器 / 信号线芯数</template>
          <div class="form-stack">
            <label>感应器个数 <el-input-number v-model="sensorCount" :min="0" controls-position="right" /></label>
            <label>非同动气缸磁环组数 <el-input-number v-model="magnetCount" :min="0" controls-position="right" /></label>
            <label>需要电缆芯数 <el-input :model-value="coreCount" readonly /></label>
          </div>
        </el-card>

        <el-card shadow="never" class="control-card pipe-list-card">
          <template #header>
            <div class="card-header-row">
              <span>管线清单</span>
              <el-button size="small" type="primary" :loading="pipeSourceRefreshing" @click="openAddDialog">新增管线</el-button>
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
            <el-table-column prop="name" label="管线 / 模块" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <div class="selection-name">
                  <el-tag v-if="row.kind === 'module'" size="small" type="warning">模块</el-tag>
                  <el-tag v-else-if="row.kind === 'component'" size="small" type="success">元件</el-tag>
                  <el-tag v-else-if="row.kind === 'pipe'" size="small">管线</el-tag>
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

        <el-card shadow="never" class="control-card">
          <template #header>运动参数</template>
          <div class="form-stack">
            <label>运动方式
              <el-select v-model="motionType">
                <el-option label="横移" value="横移" />
                <el-option label="升降" value="升降" />
              </el-select>
            </label>
            <label>移动行程 (mm) <el-input-number v-model="stroke" :min="0" controls-position="right" /></label>
            <label>固定端偏移 Lm (mm) <el-input-number v-model="lmOffset" :min="0" controls-position="right" /></label>
          </div>
        </el-card>
      </aside>

      <section class="result-area">
        <el-card shadow="never" class="result-card">
          <template #header>
            <div class="card-header-row">
              <span>型号匹配矩阵</span>
              <el-tag effect="plain">{{ brand.toUpperCase() }}</el-tag>
            </div>
          </template>
          <el-table :data="calcResult?.matchResults || []" stripe height="330" class="match-table" row-key="model" :row-class-name="matchRowClass">
            <el-table-column prop="model" label="型号" min-width="150" />
            <el-table-column prop="innerHeight" label="内高" width="82" align="center" />
            <el-table-column prop="recRadius" label="推荐R" width="88" align="center" />
            <el-table-column prop="innerArea" label="内空" width="92" align="center" />
            <el-table-column label="内高" width="78" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okHeight ? 'success' : 'danger'" size="small">{{ row.okHeight ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="弯曲R" width="82" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okRadius ? 'success' : 'danger'" size="small">{{ row.okRadius ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="内空" width="78" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okArea ? 'success' : 'danger'" size="small">{{ row.okArea ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="初判" width="82" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okPrelim ? 'success' : 'danger'" size="small">{{ row.okPrelim ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="架空能力" min-width="110" align="center">
              <template #default="{ row }">
                {{ row.calcSpan > 0 ? Math.round(row.calcSpan) : 0 }}
              </template>
            </el-table-column>
            <el-table-column label="架空" width="78" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okSpan ? 'success' : 'danger'" size="small">{{ row.okSpan ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="最终" width="78" align="center">
              <template #default="{ row }">
                <el-tag :type="row.okFinal ? 'success' : 'danger'" size="small">{{ row.okFinal ? 'OK' : 'NG' }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <el-card shadow="never" class="result-card">
          <template #header>
            <div class="card-header-row">
              <span>计算过程明细</span>
              <el-tag effect="plain">自动核算</el-tag>
            </div>
          </template>
          <div class="step-panel">
            <section class="step-block">
              <h4><span>3-1</span>核算拖链最小内高 / mm</h4>
              <div class="step-row">
                <b>最大管线外径 × 1.25</b>
                <strong>{{ calcResult?.steps.step3_1_MinHeight || '-' }}</strong>
              </div>
            </section>

            <section class="step-block">
              <h4><span>3-2</span>核算拖链推荐弯曲半径</h4>
              <div class="step-row">
                <b>气管弯曲半径核对</b>
                <strong>{{ calcResult?.steps.step3_2_BendTube || '-' }}</strong>
                <em>普通气管允许最小弯曲半径，通常 ≥ 8 × 外径</em>
              </div>
              <div class="step-row">
                <b>电缆弯曲半径核对</b>
                <strong>{{ calcResult?.steps.step3_2_BendCable || '-' }}</strong>
                <em>耐曲折电缆允许最小弯曲半径，通常 ≥ 8 × 外径</em>
              </div>
              <div class="step-row is-strong">
                <b>拖链最小弯曲半径 / mm</b>
                <strong>{{ calcResult?.steps.step3_2_BendMax || '-' }}</strong>
                <em>伺服编码器线允许最小弯曲半径，通常 ≥ 13 × 外径</em>
              </div>
              <div v-if="bendRows.length" class="bend-summary">
                <div v-for="row in bendRows" :key="row.label" :class="{ 'is-max': row.isMax }">
                  <span>{{ row.label }}</span>
                  <strong>{{ row.value }} mm{{ row.isMax ? '  控制值' : '' }}</strong>
                </div>
              </div>
            </section>

            <section class="step-block">
              <h4><span>3-3</span>核算旋转拖链内空</h4>
              <div class="step-row"><b>管线面积总和</b><strong>{{ calcResult?.steps.step3_3_AreaSum || '-' }}</strong></div>
              <div class="step-row">
                <b>管线与拖链内部面积占比</b>
                <strong>{{ calcResult?.steps.step3_3_Ratio || '-' }}</strong>
                <em>{{ brand === 'wzl' ? '无尘拖链建议内空占比 60%' : '普通拖链直角可用面积减少，建议取 55%' }}</em>
              </div>
              <div class="step-row is-strong"><b>拖链最小内部面积 / mm²</b><strong>{{ calcResult?.steps.step3_3_MinArea || '-' }}</strong></div>
            </section>

            <section class="step-block">
              <h4><span>3-4</span>初步选定拖链型号</h4>
              <div class="step-row is-strong"><b>初步选定型号</b><strong>{{ calcResult?.steps.step3_4_PrelimModel || '-' }}</strong></div>
            </section>

            <section class="step-block">
              <h4><span>3-5</span>核算架空长度</h4>
              <div class="step-row"><b>运动方式</b><strong>{{ calcResult?.steps.step3_5_Motion || '-' }}</strong></div>
              <div class="step-row"><b>移动行程</b><strong>{{ calcResult?.steps.step3_5_Stroke || '-' }}</strong></div>
              <div class="step-row"><b>拖链固定端偏移中心点距离 Lm</b><strong>{{ calcResult?.steps.step3_5_Lm || '-' }}</strong></div>
              <div class="step-row"><b>初步选定弯曲长度 Lp</b><strong>{{ calcResult?.steps.step3_5_PrelimLp || '-' }}</strong></div>
              <div class="step-row">
                <b>初步选定拖链长度 Lk</b>
                <strong>{{ calcResult?.steps.step3_5_PrelimLk || '-' }}</strong>
                <em>ROUNDUP(行程÷2 + Lm + Lp, -1)</em>
              </div>
              <div class="step-row is-strong"><b>初步选定型号 + 长度</b><strong>{{ calcResult?.steps.step3_5_PrelimFull || '-' }}</strong></div>
            </section>

            <section class="step-block">
              <h4><span>3-6</span>架空判定与最终选定</h4>
              <div class="step-row">
                <b>需要架空长度</b>
                <strong>{{ calcResult?.steps.step3_6_NeedSpan || '-' }}</strong>
                <em>横移 = 行程÷2；升降无需计算</em>
              </div>
              <div class="step-row"><b>负载重量 kg/m</b><strong>{{ calcResult?.steps.step3_6_Load || '-' }}</strong></div>
              <div class="step-row"><b>判定初选拖链架空是否满足</b><strong>{{ calcResult?.steps.step3_6_SpanOk || '-' }}</strong></div>
              <div class="step-row"><b>选定满足架空的拖链</b><strong>{{ calcResult?.steps.step3_6_FinalModel || '-' }}</strong></div>
              <div class="step-row"><b>弯曲长度 Lp（根据右表选择）</b><strong>{{ calcResult?.steps.step3_6_FinalLp || '-' }}</strong></div>
              <div class="step-row is-strong"><b>选定拖链长度 Lk</b><strong>{{ calcResult?.steps.step3_6_FinalLk || '-' }}</strong></div>
            </section>

            <section class="step-block is-strategy">
              <h4><span>4</span>架空长度超出对策</h4>
              <p><b>4-1：</b>若长度超出，可增加龙骨系统，单内空会减少，需与厂商确认管线排布；或加大拖链规格。</p>
              <p><b>4-2：</b>若长度超出，可增加辅助轮支撑，但会增加拖链磨损。</p>
            </section>
          </div>
        </el-card>

        <div class="metrics-result-row">
          <el-card shadow="never" class="result-card">
            <template #header>核算指标</template>
            <div class="metric-grid">
              <MetricItem label="最小内高" :value="calcResult?.minHeight?.toFixed(2)" unit="mm" />
              <MetricItem label="最小弯曲R" :value="calcResult?.minRadius?.toFixed(0)" unit="mm" />
              <MetricItem label="管线面积" :value="calcResult?.totalArea?.toFixed(1)" unit="mm²" />
              <MetricItem label="最小内空" :value="calcResult?.minInnerArea?.toFixed(1)" unit="mm²" />
              <MetricItem label="总重量" :value="calcResult?.totalWeight?.toFixed(4)" unit="kg/m" />
              <MetricItem label="需架空长" :value="calcResult?.needSpan || undefined" unit="mm" />
            </div>
          </el-card>

          <el-card shadow="never" class="result-card">
            <template #header>最终选定结论</template>
            <el-skeleton v-if="calcLoading" :rows="3" animated />
            <el-alert v-else-if="calcError" :title="calcError" type="error" show-icon :closable="false" />
            <div v-else class="conclusion-box" :class="calcResult?.resultStatus || 'warn'">
              <strong>{{ calcResult?.resultMessage || '请填写管线清单' }}</strong>
              <span v-if="calcResult?.finalModel">
                弯曲长度 Lp={{ calcResult.finalModel.lp }}mm · 拖链长度 Lk={{ calcResult.finalModel.lk }}mm
              </span>
              <span v-else-if="calcResult?.preliminaryModel">
                弯曲长度 Lp={{ calcResult.preliminaryModel.lp }}mm · 拖链长度 Lk={{ calcResult.preliminaryModel.lk }}mm
              </span>
              <p v-if="calcResult?.strategyNote">{{ calcResult.strategyNote }}</p>
            </div>
          </el-card>
        </div>
      </section>
    </div>

    <AddPipeDialog
      v-model="showAddDialog"
      :pipe-lib="pipeLib"
      :pipe-modules="pipeModules"
      :pipe-components="pipeComponents"
      :active-pipes="activePipes"
      @confirm="addPipes"
    />
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, ref, watch } from 'vue';
import AddPipeDialog from '../components/AddPipeDialog.vue';
import MetricItem from '../widgets/MetricItem.vue';
import PageShell from '../components/PageShell.vue';
import { calculationApi } from '../api/calculation';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeComponents } from '../composables/usePipeComponents';
import { usePipeModules } from '../composables/usePipeModules';
import type { ActivePipe, CalculationResponse } from '../types';
import { expandSelectionToPipes } from '../utils/pipeSelection';
import { createTrunkingSelectionRows } from '../utils/trunkingSelectionDisplay';

const { pipeLib, pipeMap, loadPipeLib } = usePipeLibrary();
const { pipeModules, moduleMap, loadPipeModules } = usePipeModules();
const { pipeComponents, componentMap, loadPipeComponents } = usePipeComponents();
const brand = ref<'wzl' | 'me'>('wzl');
const sensorCount = ref(0);
const magnetCount = ref(0);
const motionType = ref<'横移' | '升降'>('横移');
const stroke = ref(0);
const lmOffset = ref(0);
const activePipes = ref<ActivePipe[]>([]);
const calcResult = ref<CalculationResponse | null>(null);
const calcLoading = ref(false);
const calcError = ref('');
const showAddDialog = ref(false);
const pipeSourceRefreshing = ref(false);
const mounted = ref(false);

const coreCount = computed(
  () => {
    if (sensorCount.value <= 0 && magnetCount.value <= 0) return 0;
    return sensorCount.value + Math.ceil(sensorCount.value / 3) * 2 + Math.ceil(magnetCount.value / 3) * 2 + 2;
  }
);

const enrichedPipes = computed(() =>
  createTrunkingSelectionRows(activePipes.value, pipeLib.value, pipeModules.value, pipeComponents.value, { areaMode: 'circle' })
);

const maxBend = computed(() => {
  let value = 0;
  activePipes.value.forEach(pipe => {
    if (pipe.kind === 'module') {
      const module = moduleMap.value[pipe.moduleId];
      module?.items.forEach(moduleItem => {
        const item = pipeMap.value[moduleItem.pipeTypeId] || moduleItem.pipeType;
        if (!item || !pipe.qty || !moduleItem.qty) return;
        value = Math.max(value, item.diameter * item.bendMultiplier);
      });
      return;
    }

    if (pipe.kind === 'component') {
      const component = componentMap.value[pipe.componentId];
      component?.items.forEach(componentItem => {
        const item = pipeMap.value[componentItem.pipeTypeId] || componentItem.pipeType;
        if (!item || !pipe.qty || !componentItem.qty) return;
        value = Math.max(value, item.diameter * item.bendMultiplier);
      });
      return;
    }

    const item = pipeMap.value[pipe.libId];
    if (!item || !pipe.qty) return;
    value = Math.max(value, item.diameter * item.bendMultiplier);
  });
  return value;
});

const bendRows = computed(() => {
  const rows = [
    { label: '气管弯曲半径', value: calcResult.value?.tubeBend || 0 },
    { label: '电缆弯曲半径', value: calcResult.value?.cableBend || 0 },
    { label: '编码器线弯曲半径', value: calcResult.value?.encoderBend || 0 }
  ];
  return rows
    .filter(row => row.value > 0)
    .map(row => ({ ...row, isMax: row.value === maxBend.value }));
});

function handleBrandChange() {
  activePipes.value = [];
  sensorCount.value = 0;
  magnetCount.value = 0;
  stroke.value = 0;
  lmOffset.value = 0;
}

async function calculate() {
  calcLoading.value = true;
  calcError.value = '';
  try {
    calcResult.value = await calculationApi.calc({
      brand: brand.value,
      sensorCount: sensorCount.value,
      magnetCount: magnetCount.value,
      motionType: motionType.value,
      stroke: stroke.value,
      lmOffset: lmOffset.value,
      pipes: expandSelectionToPipes(activePipes.value, pipeModules.value, pipeComponents.value)
    });
  } catch (err) {
    calcError.value = err instanceof Error ? err.message : '计算失败';
  } finally {
    calcLoading.value = false;
  }
}

function updateQty(index: number, qty: number) {
  activePipes.value[index] = { ...activePipes.value[index], qty };
}

function removePipe(index: number) {
  activePipes.value.splice(index, 1);
}

async function refreshPipeSources() {
  await Promise.all([loadPipeLib(), loadPipeModules(), loadPipeComponents()]);
}

async function refreshPipeSourcesForPicker() {
  pipeSourceRefreshing.value = true;
  try {
    await refreshPipeSources();
    return true;
  } catch (err) {
    calcError.value = err instanceof Error ? err.message : '管线库加载失败';
    return false;
  } finally {
    pipeSourceRefreshing.value = false;
  }
}

async function openAddDialog() {
  if (!await refreshPipeSourcesForPicker()) return;
  showAddDialog.value = true;
}

function selectionRowClass({ row }: { row: { kind?: string } }) {
  return row.kind === 'module-item' ? 'module-detail-row' : '';
}

function addPipes(payload: { pipeIds: number[]; moduleIds: number[]; componentIds: number[] }) {
  const existingPipeIds = new Set(activePipes.value.filter(isPipeSelection).map(pipe => pipe.libId));
  const existingModuleIds = new Set(activePipes.value.filter(pipe => pipe.kind === 'module').map(pipe => pipe.moduleId));
  const existingComponentIds = new Set(activePipes.value.filter(pipe => pipe.kind === 'component').map(pipe => pipe.componentId));
  activePipes.value.push(
    ...payload.pipeIds
      .filter(id => !existingPipeIds.has(id))
      .map(id => ({ kind: 'pipe' as const, libId: id, qty: 1 })),
    ...payload.moduleIds
      .filter(id => !existingModuleIds.has(id))
      .map(id => ({ kind: 'module' as const, moduleId: id, qty: 1 })),
    ...payload.componentIds
      .filter(id => !existingComponentIds.has(id))
      .map(id => ({ kind: 'component' as const, componentId: id, qty: 1 }))
  );
}

function isPipeSelection(pipe: ActivePipe): pipe is Extract<ActivePipe, { kind?: 'pipe' }> {
  return !pipe.kind || pipe.kind === 'pipe';
}

function matchRowClass({ row }: { row: { okFinal: boolean } }) {
  return row.okFinal ? 'is-final-match' : '';
}

let timer = 0;
watch([brand, sensorCount, magnetCount, motionType, stroke, lmOffset, activePipes], () => {
  window.clearTimeout(timer);
  timer = window.setTimeout(calculate, 300);
}, { deep: true });

onMounted(async () => {
  await refreshPipeSources();
  await calculate();
  mounted.value = true;
});

onActivated(async () => {
  if (!mounted.value) return;
  await refreshPipeSources();
  await calculate();
});
</script>

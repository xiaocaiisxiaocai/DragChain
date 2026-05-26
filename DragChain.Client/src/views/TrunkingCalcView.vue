<template>
  <PageShell>
    <div class="trunking-layout">
      <section class="trunking-slot-board">
        <el-card shadow="never" class="control-card">
          <template #header>
            <div class="card-header-row">
              <span>选型参数</span>
              <el-button size="small" type="primary" @click="addSlot">
                新增{{ activeSlotLayout === 'leftRight' ? '左右' : '上下' }}槽位
              </el-button>
            </div>
          </template>
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

        <el-tabs v-model="activeSlotLayout" class="slot-tabs">
          <el-tab-pane label="左右槽位" name="leftRight" />
          <el-tab-pane label="上下槽位" name="topBottom" />
        </el-tabs>

        <el-scrollbar class="slot-scroll">
          <el-empty v-if="!visibleSlots.length" :description="`请先新增${activeSlotLayout === 'leftRight' ? '左右' : '上下'}槽位`" />
          <div v-else class="slot-list">
            <div v-for="slot in visibleSlots" :key="slot.id" class="slot-pair-row">
              <el-card shadow="never" class="slot-card">
                <template #header>
                  <div class="slot-header">
                    <el-input v-model="slot.name" class="slot-name-input" placeholder="槽位名称" />
                    <el-button link type="danger" @click="removeSlot(slot.id)">删除</el-button>
                  </div>
                </template>

                <div v-if="slot.layout === 'leftRight'" class="slot-section-grid is-single">
                  <section class="slot-section">
                    <div class="section-title">
                      <span>管线清单（自动分左右）</span>
                      <span class="area-summary">{{ getSlotAreaSummary(slot).totalAreaText }} mm²</span>
                      <span class="area-summary is-muted">左 {{ getSlotAreaSummary(slot).leftAreaText }} / 右 {{ getSlotAreaSummary(slot).rightAreaText }}</span>
                      <el-button size="small" @click="openPipePicker(slot.id, 'left')">添加管线</el-button>
                    </div>
                    <div class="trunking-select-row">
                      <span>左侧线槽</span>
                      <el-select
                        :model-value="getDisplayedSlotTrunkingId(slot, 'left')"
                        size="small"
                        filterable
                        placeholder="选择左侧线槽"
                        @change="(value: number) => updateSlotTrunkingId(slot.id, 'left', value)"
                      >
                        <el-option
                          v-for="item in trunkingCatalog"
                          :key="item.id"
                          :label="formatTrunkingOption(item)"
                          :value="item.id"
                        />
                      </el-select>
                      <span>右侧线槽</span>
                      <el-select
                        :model-value="getDisplayedSlotTrunkingId(slot, 'right')"
                        size="small"
                        filterable
                        placeholder="选择右侧线槽"
                        @change="(value: number) => updateSlotTrunkingId(slot.id, 'right', value)"
                      >
                        <el-option
                          v-for="item in trunkingCatalog"
                          :key="item.id"
                          :label="formatTrunkingOption(item)"
                          :value="item.id"
                        />
                      </el-select>
                    </div>
                    <el-table :data="createSlotRows(slot.pipes)" size="small" border max-height="260">
                      <el-table-column prop="name" label="管线" min-width="180" show-overflow-tooltip />
                      <el-table-column prop="qty" label="数量" width="96" align="center">
                        <template #default="{ row }">
                          <el-input-number
                            class="pipe-qty-input"
                            :model-value="row.qty"
                            :min="0"
                            :step="1"
                            size="small"
                            controls-position="right"
                            @update:model-value="(value: number | undefined) => updateSlotPipeQty(slot.id, row.sourceIndex, Number(value || 0))"
                          />
                        </template>
                      </el-table-column>
                      <el-table-column prop="sideLabel" label="分侧" width="88" align="center" />
                      <el-table-column prop="areaText" label="面积" width="104" align="right" />
                      <el-table-column width="64" align="center">
                        <template #default="{ row }">
                          <el-button link type="danger" @click="removeSlotPipe(slot.id, row.sourceIndex)">删</el-button>
                        </template>
                      </el-table-column>
                    </el-table>
                  </section>
                </div>

                <div v-else class="slot-section-grid">
                  <section v-for="section in slot.sections" :key="section.key" class="slot-section">
                    <div class="section-title">
                      <span>{{ section.label }}</span>
                      <span class="area-summary">{{ getSectionAreaSummary(section).totalAreaText }} mm²</span>
                      <el-button size="small" @click="openPipePicker(slot.id, section.key)">添加管线</el-button>
                    </div>
                    <div class="trunking-select-row is-single">
                      <span>线槽</span>
                      <el-select
                        :model-value="getDisplayedSectionTrunkingId(slot, section)"
                        size="small"
                        filterable
                        placeholder="选择线槽"
                        @change="(value: number) => updateTopBottomSectionTrunkingId(slot.id, section.key, value)"
                      >
                        <el-option
                          v-for="item in trunkingCatalog"
                          :key="item.id"
                          :label="formatTrunkingOption(item)"
                          :value="item.id"
                        />
                      </el-select>
                    </div>
                    <el-table :data="createSlotRows(section.pipes)" size="small" border max-height="220">
                      <el-table-column prop="name" label="管线" min-width="180" show-overflow-tooltip />
                      <el-table-column prop="qty" label="数量" width="96" align="center">
                        <template #default="{ row }">
                          <el-input-number
                            class="pipe-qty-input"
                            :model-value="row.qty"
                            :min="0"
                            :step="1"
                            size="small"
                            controls-position="right"
                            @update:model-value="(value: number | undefined) => updateSectionPipeQty(slot.id, section.key, row.sourceIndex, Number(value || 0))"
                          />
                        </template>
                      </el-table-column>
                      <el-table-column prop="areaText" label="面积" width="104" align="right" />
                      <el-table-column width="64" align="center">
                        <template #default="{ row }">
                          <el-button link type="danger" @click="removeSectionPipe(slot.id, section.key, row.sourceIndex)">删</el-button>
                        </template>
                      </el-table-column>
                    </el-table>
                  </section>
                </div>
              </el-card>

              <div class="slot-summary-column">
                <el-alert v-if="error" :title="error" type="error" show-icon :closable="false" class="summary-error" />
                <div
                  v-for="item in getSlotSummaryItems(slot.id)"
                  :key="item.key"
                  class="summary-item"
                  :class="item.section.resultStatus"
                >
                  <button type="button" class="summary-main" @click="openDetail(item)">
                    <span class="summary-title">{{ item.slot.name }} / {{ item.section.label }}</span>
                    <span class="summary-model">{{ item.section.selectedTrunking?.model || '-' }}</span>
                    <strong>{{ item.section.selectedTrunking ? `${(item.section.actualFillRatio * 100).toFixed(1)}%` : '-' }}</strong>
                    <small>{{ item.section.resultMessage }}</small>
                  </button>
                  <div class="summary-fill-editor">
                    <span>上限</span>
                    <el-input-number
                      :model-value="getSectionFillRatio(item)"
                      size="small"
                      :min="1"
                      :max="100"
                      :step="1"
                      controls-position="right"
                      @click.stop
                      @update:model-value="(value: number | undefined) => updateSummaryFillRatio(item, Number(value || fillRatio))"
                    />
                    <span>%</span>
                    <el-text v-if="workspaceSaving" size="small" type="info">保存中</el-text>
                    <el-text v-else-if="workspaceSaved" size="small" type="success">已保存</el-text>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </el-scrollbar>
      </section>
    </div>

    <AddPipeDialog
      v-model="showAddDialog"
      :pipe-lib="trunkingPipeLib"
      :pipe-modules="trunkingModules"
      :pipe-components="trunkingComponents"
      :active-pipes="pickerActivePipes"
      :allowed-types="TRUNKING_ALLOWED_TYPES"
      @confirm="addPipes"
    />

    <el-dialog v-model="detailVisible" :title="detailTitle" width="820px" class="trunking-detail-dialog">
      <div v-if="detailItem" class="detail-metrics">
        <MetricItem label="管线面积" :value="detailItem.section.totalArea.toFixed(0)" unit="mm²" />
        <MetricItem label="当前线槽" :value="detailItem.section.selectedTrunking?.model || '-'" />
        <MetricItem
          label="实际填充率"
          :value="detailItem.section.selectedTrunking ? (detailItem.section.actualFillRatio * 100).toFixed(1) : undefined"
          unit="%"
        />
      </div>
      <el-table v-if="detailItem" :data="detailItem.section.matchResults" size="small" border max-height="360">
        <el-table-column prop="model" label="线槽型号" min-width="130" show-overflow-tooltip />
        <el-table-column label="尺寸" width="100">
          <template #default="{ row }">{{ row.width }}×{{ row.height }}</template>
        </el-table-column>
        <el-table-column label="面积" width="90" align="right">
          <template #default="{ row }">{{ row.crossSection.toFixed(0) }}</template>
        </el-table-column>
        <el-table-column label="填充率" width="90" align="right">
          <template #default="{ row }">{{ (row.actualFillRatio * 100).toFixed(1) }}%</template>
        </el-table-column>
        <el-table-column label="结果" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.okFill ? 'success' : 'danger'" size="small">{{ row.result }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="推荐" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isRecommended" type="primary" size="small">推荐</el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import AddPipeDialog from '../components/AddPipeDialog.vue';
import MetricItem from '../widgets/MetricItem.vue';
import PageShell from '../components/PageShell.vue';
import { trunkingApi } from '../api/trunking';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeComponents } from '../composables/usePipeComponents';
import { usePipeModules } from '../composables/usePipeModules';
import type { ActivePipe, PipeType, TrunkingCalcResponse, TrunkingCatalog, TrunkingSideResult, TrunkingSlotRequest, TrunkingSlotResult } from '../types';
import { expandSelectionToPipes } from '../utils/pipeSelection';
import { getPipeDisplayType } from '../utils/pipeType';
import { createTrunkingSelectionRows, summarizeTrunkingSelectionRows, type TrunkingSelectionRow } from '../utils/trunkingSelectionDisplay';
import { getTrunkingRuntimeState, setTrunkingRuntimeState, type LocalSlot, type LocalSlotSection, type SlotLayout, type SlotSectionKey } from '../stores/trunkingRuntimeState';

interface SummaryItem {
  key: string;
  slot: TrunkingSlotResult;
  section: TrunkingSideResult;
}

const TRUNKING_ALLOWED_TYPES = ['weak_cable', 'strong_cable', 'encoder'];

const { pipeLib, loadPipeLib } = usePipeLibrary();
const { pipeModules, loadPipeModules } = usePipeModules();
const { pipeComponents, loadPipeComponents } = usePipeComponents();
const fillRatio = ref(75);
const activeSlotLayout = ref<SlotLayout>('leftRight');
const slots = ref<LocalSlot[]>([]);
const trunkingCatalog = ref<TrunkingCatalog[]>([]);
const result = ref<TrunkingCalcResponse | null>(null);
const loading = ref(false);
const error = ref('');
const settingsLoading = ref(false);
const settingsSaving = ref(false);
const settingsSaved = ref(false);
const workspaceSaving = ref(false);
const workspaceSaved = ref(false);
const showAddDialog = ref(false);
const pickerTarget = reactive<{ slotId: string; sectionKey: SlotSectionKey }>({ slotId: '', sectionKey: 'left' });
const detailVisible = ref(false);
const detailItem = ref<SummaryItem | null>(null);

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
const trunkingComponents = computed(() => pipeComponents.value.map(component => ({
  ...component,
  items: component.items.filter(item => {
    const pipe = pipeLib.value.find(pipeItem => pipeItem.id === item.pipeTypeId) || item.pipeType;
    return isTrunkingPipe(pipe);
  })
})).filter(component => component.items.length > 0));
const summaryItems = computed<SummaryItem[]>(() =>
  (result.value?.slots || []).flatMap(slot =>
    slot.sections.map(section => ({
      key: `${slot.id}-${section.key}`,
      slot,
      section
    }))
  )
);
const visibleSlots = computed(() => slots.value.filter(slot => slot.layout === activeSlotLayout.value));
const detailTitle = computed(() => detailItem.value ? `${detailItem.value.slot.name} / ${detailItem.value.section.label}` : '线槽详情');
const pickerActivePipes = computed(() => {
  const slot = slots.value.find(item => item.id === pickerTarget.slotId);
  if (!slot) return [];
  if (slot.layout === 'leftRight') return slot.pipes;
  return slot.sections.find(section => section.key === pickerTarget.sectionKey)?.pipes || [];
});

function getSlotSummaryItems(slotId: string) {
  return summaryItems.value.filter(item => item.slot.id === slotId);
}

function getResultSection(slotId: string, sectionKey: string) {
  return result.value?.slots
    .find(slot => slot.id === slotId)
    ?.sections.find(section => section.key === sectionKey) || null;
}

function getDisplayedSlotTrunkingId(slot: LocalSlot, sectionKey: 'left' | 'right') {
  const manualId = sectionKey === 'left' ? slot.leftTrunkingId : slot.rightTrunkingId;
  return manualId ?? getResultSection(slot.id, sectionKey)?.selectedTrunking?.id ?? undefined;
}

function getDisplayedSectionTrunkingId(slot: LocalSlot, section: LocalSlotSection) {
  return section.selectedTrunkingId ?? getResultSection(slot.id, section.key)?.selectedTrunking?.id ?? undefined;
}

function getSectionFillRatio(item: SummaryItem) {
  const slot = slots.value.find(localSlot => localSlot.id === item.slot.id);
  if (!slot) return fillRatio.value;

  if (slot.layout === 'leftRight') {
    return item.section.key === 'left' ? slot.leftFillRatio ?? fillRatio.value : slot.rightFillRatio ?? fillRatio.value;
  }

  return slot.sections.find(section => section.key === item.section.key)?.fillRatio ?? fillRatio.value;
}

function updateSummaryFillRatio(item: SummaryItem, value: number) {
  const ratio = Math.min(Math.max(value, 1), 100);
  const slot = slots.value.find(localSlot => localSlot.id === item.slot.id);
  if (!slot) return;

  if (slot.layout === 'leftRight') {
    if (item.section.key === 'left') slot.leftFillRatio = ratio;
    else slot.rightFillRatio = ratio;
  } else {
    const section = slot.sections.find(localSection => localSection.key === item.section.key);
    if (section) section.fillRatio = ratio;
  }

  queueCalculate();
  queueSaveWorkspaceStatus();
}

function addSlot() {
  const layout = activeSlotLayout.value;
  const index = slots.value.filter(slot => slot.layout === layout).length + 1;
  slots.value.push(createSlot(`${layout === 'leftRight' ? '左右' : '上下'}槽位${index}`, layout, fillRatio.value));
  queueCalculate();
}

function createSlot(name: string, layout: SlotLayout, defaultFillRatio: number): LocalSlot {
  return {
    id: `slot-${Date.now()}-${Math.random().toString(16).slice(2)}`,
    name,
    layout,
    leftTrunkingId: null,
    rightTrunkingId: null,
    leftFillRatio: defaultFillRatio,
    rightFillRatio: defaultFillRatio,
    pipes: [],
    sections: [
      { key: 'top', label: '上层', selectedTrunkingId: null, fillRatio: defaultFillRatio, pipes: [] },
      { key: 'bottom', label: '下层', selectedTrunkingId: null, fillRatio: defaultFillRatio, pipes: [] }
    ]
  };
}

function formatTrunkingOption(item: TrunkingCatalog) {
  return `${item.model}（${item.width}×${item.height}，${item.crossSection}mm²）`;
}

function removeSlot(slotId: string) {
  slots.value = slots.value.filter(slot => slot.id !== slotId);
  queueCalculate();
}

function openPipePicker(slotId: string, sectionKey: SlotSectionKey) {
  pickerTarget.slotId = slotId;
  pickerTarget.sectionKey = sectionKey;
  showAddDialog.value = true;
}

function addPipes(payload: { pipeIds: number[]; moduleIds: number[]; componentIds: number[] }) {
  const target = getTargetPipeList();
  if (!target) return;
  const existingPipeIds = new Set(target.filter(isPipeSelection).map(pipe => pipe.libId));
  const existingModuleIds = new Set(target.filter(pipe => pipe.kind === 'module').map(pipe => pipe.moduleId));
  const existingComponentIds = new Set(target.filter(pipe => pipe.kind === 'component').map(pipe => pipe.componentId));

  target.push(
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
  queueCalculate();
}

function getTargetPipeList() {
  const slot = slots.value.find(item => item.id === pickerTarget.slotId);
  if (!slot) return null;
  if (slot.layout === 'leftRight') return slot.pipes;
  return slot.sections.find(section => section.key === pickerTarget.sectionKey)?.pipes || null;
}

function createSlotRows(activePipes: ActivePipe[]): TrunkingSelectionRow[] {
  return createTrunkingSelectionRows(activePipes, pipeLib.value, trunkingModules.value, trunkingComponents.value);
}

function updateSlotTrunkingId(slotId: string, sectionKey: 'left' | 'right', value: number | undefined) {
  const slot = slots.value.find(item => item.id === slotId);
  if (!slot) return;

  if (sectionKey === 'left') slot.leftTrunkingId = value ?? null;
  else slot.rightTrunkingId = value ?? null;

  queueCalculate();
}

function updateTopBottomSectionTrunkingId(slotId: string, sectionKey: string, value: number | undefined) {
  const section = slots.value.find(item => item.id === slotId)?.sections.find(item => item.key === sectionKey);
  if (!section) return;

  section.selectedTrunkingId = value ?? null;
  queueCalculate();
}

function getSlotAreaSummary(slot: LocalSlot) {
  return summarizeTrunkingSelectionRows(createSlotRows(slot.pipes));
}

function getSectionAreaSummary(section: LocalSlotSection) {
  return summarizeTrunkingSelectionRows(createSlotRows(section.pipes));
}

function updateSlotPipeQty(slotId: string, index: number, qty: number) {
  const slot = slots.value.find(item => item.id === slotId);
  if (slot) slot.pipes[index] = { ...slot.pipes[index], qty };
  queueCalculate();
}

function updateSectionPipeQty(slotId: string, sectionKey: string, index: number, qty: number) {
  const section = slots.value.find(item => item.id === slotId)?.sections.find(item => item.key === sectionKey);
  if (section) section.pipes[index] = { ...section.pipes[index], qty };
  queueCalculate();
}

function removeSlotPipe(slotId: string, index: number) {
  slots.value.find(item => item.id === slotId)?.pipes.splice(index, 1);
  queueCalculate();
}

function removeSectionPipe(slotId: string, sectionKey: string, index: number) {
  slots.value.find(item => item.id === slotId)?.sections.find(item => item.key === sectionKey)?.pipes.splice(index, 1);
  queueCalculate();
}

function toRequestSlots(): TrunkingSlotRequest[] {
  return slots.value
    .filter(slot => slot.name.trim())
    .map(slot => {
      if (slot.layout === 'leftRight') {
        return {
          id: slot.id,
          name: slot.name.trim(),
          layout: slot.layout,
          leftTrunkingId: slot.leftTrunkingId,
          rightTrunkingId: slot.rightTrunkingId,
          leftFillRatio: toRatioValue(slot.leftFillRatio),
          rightFillRatio: toRatioValue(slot.rightFillRatio),
          pipes: expandSelectionToPipes(slot.pipes, trunkingModules.value, trunkingComponents.value)
        };
      }

      return {
        id: slot.id,
        name: slot.name.trim(),
        layout: slot.layout,
        sections: slot.sections.map(section => ({
          key: section.key,
          label: section.label,
          selectedTrunkingId: section.selectedTrunkingId,
          fillRatio: toRatioValue(section.fillRatio),
          pipes: expandSelectionToPipes(section.pipes, trunkingModules.value, trunkingComponents.value)
        }))
      };
    });
}

function toRatioValue(value: number | null) {
  return value && value > 0 ? value / 100 : null;
}

async function calculate() {
  loading.value = true;
  error.value = '';
  try {
    result.value = await trunkingApi.calculate({
      selectedTrunkingId: 0,
      fillRatio: fillRatio.value / 100,
      pipes: [],
      slots: toRequestSlots()
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
    previousGlobalFillRatio = fillRatio.value;
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

function loadWorkspace() {
  const runtimeState = getTrunkingRuntimeState();
  if (!runtimeState) return;

  activeSlotLayout.value = runtimeState.activeSlotLayout;
  slots.value = runtimeState.slots;
}

function saveWorkspace() {
  setTrunkingRuntimeState(createWorkspaceState());
}

function queueSaveWorkspaceStatus() {
  workspaceSaving.value = true;
  workspaceSaved.value = false;
  window.clearTimeout(saveWorkspaceStatusTimer);
  saveWorkspaceStatusTimer = window.setTimeout(() => {
    saveWorkspace();
    workspaceSaving.value = false;
    workspaceSaved.value = true;
    window.setTimeout(() => {
      workspaceSaved.value = false;
    }, 1600);
  }, 600);
}

function createWorkspaceState() {
  return {
    activeSlotLayout: activeSlotLayout.value,
    slots: slots.value
  };
}

function openDetail(item: SummaryItem) {
  detailItem.value = item;
  detailVisible.value = true;
}

function isPipeSelection(pipe: ActivePipe): pipe is Extract<ActivePipe, { kind?: 'pipe' }> {
  return !pipe.kind || pipe.kind === 'pipe';
}

let calculateTimer = 0;
let saveSettingsTimer = 0;
let saveWorkspaceStatusTimer = 0;
let previousGlobalFillRatio = fillRatio.value;

function queueCalculate() {
  window.clearTimeout(calculateTimer);
  calculateTimer = window.setTimeout(calculate, 300);
}

watch(slots, () => {
  queueCalculate();
  saveWorkspace();
}, { deep: true });

watch(fillRatio, () => {
  syncDefaultSectionFillRatios(previousGlobalFillRatio, fillRatio.value);
  previousGlobalFillRatio = fillRatio.value;
  window.clearTimeout(calculateTimer);
  window.clearTimeout(saveSettingsTimer);
  calculateTimer = window.setTimeout(calculate, 300);
  saveSettingsTimer = window.setTimeout(saveSettings, 600);
});

watch(activeSlotLayout, saveWorkspace);

function syncDefaultSectionFillRatios(previousValue: number, nextValue: number) {
  slots.value.forEach(slot => {
    if (slot.leftFillRatio === previousValue) slot.leftFillRatio = nextValue;
    if (slot.rightFillRatio === previousValue) slot.rightFillRatio = nextValue;
    slot.sections.forEach(section => {
      if (section.fillRatio === previousValue) section.fillRatio = nextValue;
    });
  });
}

onMounted(async () => {
  const [catalog] = await Promise.all([trunkingApi.getAll(), loadSettings(), loadPipeLib(), loadPipeModules(), loadPipeComponents()]);
  trunkingCatalog.value = catalog;
  loadWorkspace();
  await calculate();
});
</script>

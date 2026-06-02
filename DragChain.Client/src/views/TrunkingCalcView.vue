<template>
  <PageShell>
    <div class="trunking-layout">
      <section class="trunking-slot-board">
        <el-card shadow="never" class="control-card">
          <template #header>
            <div class="card-header-row">
              <span>选型参数</span>
              <el-button size="small" type="primary" @click="addSlot">
                添加槽位
              </el-button>
            </div>
          </template>
        </el-card>

        <el-scrollbar class="slot-scroll">
          <el-empty v-if="!slots.length" description="请先添加槽位" />
          <div v-else class="trunking-stack">
            <el-alert v-if="error" :title="error" type="error" show-icon :closable="false" class="summary-error" />

            <template v-for="(slot, index) in slots" :key="slot.id">
              <div class="trunking-stack-row is-segment">
                <button
                  v-if="getHorizontalSegment(index)"
                  type="button"
                  class="segment-name"
                  :class="getHorizontalSegment(index)?.resultStatus"
                  @click="openSegmentDetail(getHorizontalSegment(index)!)"
                >
                  <strong>{{ getHorizontalSegment(index)?.name }}</strong>
                  <small>{{ getSegmentTrunkingSummary(getHorizontalSegment(index)!) }}</small>
                  <span class="segment-inline-actions">
                    <span>管线面积 {{ getResultAreaText(getHorizontalSegment(index)!) }} mm²</span>
                  </span>
                </button>
              </div>

              <div class="slot-layout-row">
                <button
                  v-if="getSideSection(index, 'left')"
                  type="button"
                  class="segment-side"
                  :class="getSideSection(index, 'left')?.resultStatus"
                  @click="openDetail({ key: `${getSideSlot(index)?.id}-left`, slot: getSideSlot(index)!, section: getSideSection(index, 'left')! })"
                >
                  <span>左</span>
                  <strong>{{ getSideSection(index, 'left')?.selectedTrunking?.model || '-' }}</strong>
                  <small>{{ getSideSection(index, 'left')?.selectedTrunking ? `${(getSideSection(index, 'left')!.actualFillRatio * 100).toFixed(1)}%` : getSideSection(index, 'left')?.resultMessage }}</small>
                  <em>管线面积 {{ getSideAreaText(index, 'left') }} mm²</em>
                </button>

                <el-card shadow="never" class="slot-card">
                  <template #header>
                    <div class="slot-header">
                      <el-input v-model="slot.name" class="slot-name-input" :placeholder="`槽位${index + 1}`" />
                      <el-button link type="danger" @click="removeSlot(slot.id)">删除</el-button>
                    </div>
                  </template>
                  <div class="slot-section">
                    <div class="section-title">
                      <span>管线清单</span>
                      <el-button size="small" @click="openSlotPipePicker(slot.id)">添加管线</el-button>
                    </div>
                    <el-table :data="createSlotCombinedRows(slot)" size="small" border max-height="260" empty-text="暂无管线">
                      <el-table-column prop="name" label="管线" min-width="170" show-overflow-tooltip>
                        <template #default="{ row: pipeRow }">
                          <span class="pipe-name-cell">
                            <span>{{ pipeRow.name }}</span>
                            <el-button
                              v-if="pipeRow.canExpand"
                              link
                              size="small"
                              type="primary"
                              @click="openPipeDetail(pipeRow)"
                            >
                              明细
                            </el-button>
                          </span>
                        </template>
                      </el-table-column>
                      <el-table-column prop="qty" label="数量" width="92" align="center">
                        <template #default="{ row: pipeRow }">
                          <el-input-number
                            class="pipe-qty-input"
                            :model-value="pipeRow.qty"
                            :min="0"
                            :step="1"
                            size="small"
                            controls-position="right"
                            @update:model-value="(value: number | undefined) => updateSlotLayerPipeQty(slot.id, pipeRow.sectionKey, pipeRow.sourceIndex, Number(value || 0))"
                          />
                        </template>
                      </el-table-column>
                      <el-table-column prop="sideLabel" label="分侧" width="78" align="center" />
                      <el-table-column prop="areaText" label="面积" width="96" align="right" />
                      <el-table-column width="58" align="center">
                        <template #default="{ row: pipeRow }">
                          <el-button link type="danger" @click="removeSlotLayerPipe(slot.id, pipeRow.sectionKey, pipeRow.sourceIndex)">删</el-button>
                        </template>
                      </el-table-column>
                    </el-table>
                  </div>
                </el-card>

                <button
                  v-if="getSideSection(index, 'right')"
                  type="button"
                  class="segment-side"
                  :class="getSideSection(index, 'right')?.resultStatus"
                  @click="openDetail({ key: `${getSideSlot(index)?.id}-right`, slot: getSideSlot(index)!, section: getSideSection(index, 'right')! })"
                >
                  <span>右</span>
                  <strong>{{ getSideSection(index, 'right')?.selectedTrunking?.model || '-' }}</strong>
                  <small>{{ getSideSection(index, 'right')?.selectedTrunking ? `${(getSideSection(index, 'right')!.actualFillRatio * 100).toFixed(1)}%` : getSideSection(index, 'right')?.resultMessage }}</small>
                  <em>管线面积 {{ getSideAreaText(index, 'right') }} mm²</em>
                </button>
              </div>
            </template>

            <div class="trunking-stack-row is-segment">
              <button
                v-if="getHorizontalSegment(slots.length)"
                type="button"
                class="segment-name"
                :class="getHorizontalSegment(slots.length)?.resultStatus"
                @click="openSegmentDetail(getHorizontalSegment(slots.length)!)"
              >
                <strong>{{ getHorizontalSegment(slots.length)?.name }}</strong>
                <small>{{ getSegmentTrunkingSummary(getHorizontalSegment(slots.length)!) }}</small>
                <span class="segment-inline-actions">
                  <span>管线面积 {{ getResultAreaText(getHorizontalSegment(slots.length)!) }} mm²</span>
                </span>
              </button>
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
        <el-table-column label="上限" width="80" align="right">
          <template #default="{ row }">{{ formatFillRatio(row.fillRatioLimit) }}</template>
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

    <el-dialog v-model="pipeDetailVisible" :title="pipeDetailTitle" width="min(1040px, 92vw)" class="pipe-detail-dialog">
      <el-table :data="pipeDetailRows" size="small" border max-height="520">
        <el-table-column prop="name" label="管线" min-width="280" show-overflow-tooltip />
        <el-table-column prop="layerLabel" label="上下" width="72" align="center" />
        <el-table-column prop="unitQtyText" label="数量" width="90" align="center" />
        <el-table-column prop="sideLabel" label="分侧" width="90" align="center" />
        <el-table-column prop="sizeText" label="尺寸" width="110" />
        <el-table-column prop="areaText" label="面积" width="96" align="right" />
      </el-table>
    </el-dialog>
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, reactive, ref, watch } from 'vue';
import AddPipeDialog from '../components/AddPipeDialog.vue';
import MetricItem from '../widgets/MetricItem.vue';
import PageShell from '../components/PageShell.vue';
import { trunkingApi } from '../api/trunking';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeComponents } from '../composables/usePipeComponents';
import { usePipeModules } from '../composables/usePipeModules';
import type { ActivePipe, PipeType, TrunkingCalcResponse, TrunkingSideResult, TrunkingSlotRequest, TrunkingSlotResult } from '../types';
import { expandSelectionToPipes } from '../utils/pipeSelection';
import { getPipeDisplayType } from '../utils/pipeType';
import { createTrunkingSelectionRows, summarizeTrunkingSelectionRows, type TrunkingSelectionDetailRow, type TrunkingSelectionRow } from '../utils/trunkingSelectionDisplay';
import { getTrunkingRuntimeState, setTrunkingRuntimeState, type LocalSlot } from '../stores/trunkingRuntimeState';
import { getOrderedSegmentLayerRefs, type OrderedSegmentLayerRef } from '../utils/trunkingSegmentLayers';

interface SummaryItem {
  key: string;
  slot: TrunkingSlotResult;
  section: TrunkingSideResult;
}

const TRUNKING_ALLOWED_TYPES = ['weak_cable', 'strong_cable', 'encoder'];

const { pipeLib, loadPipeLib } = usePipeLibrary();
const { pipeModules, loadPipeModules } = usePipeModules();
const { pipeComponents, loadPipeComponents } = usePipeComponents();
const slots = ref<LocalSlot[]>([]);
const result = ref<TrunkingCalcResponse | null>(null);
const loading = ref(false);
const error = ref('');
const workspaceSaving = ref(false);
const workspaceSaved = ref(false);
const showAddDialog = ref(false);
const nextSlotNumber = ref(1);
const pickerTarget = reactive<{ slotId: string; sectionKey: 'top' | 'bottom' }>({ slotId: '', sectionKey: 'top' });
const detailVisible = ref(false);
const detailItem = ref<SummaryItem | null>(null);
const mounted = ref(false);
const pipeDetailVisible = ref(false);
const pipeDetailItem = ref<TrunkingSelectionRow | null>(null);

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
const detailTitle = computed(() => detailItem.value ? getDetailTitle(detailItem.value) : '线槽详情');
const pipeDetailTitle = computed(() => pipeDetailItem.value ? `${pipeDetailItem.value.name} 内部管线` : '内部管线');
const pipeDetailRows = computed<TrunkingSelectionDetailRow[]>(() => pipeDetailItem.value?.children || []);
const pickerActivePipes = computed(() => {
  const slot = slots.value.find(item => item.id === pickerTarget.slotId);
  return slot?.sections.find(section => section.key === pickerTarget.sectionKey)?.pipes || [];
});
function addSlot() {
  const slotNumber = nextSlotNumber.value;
  nextSlotNumber.value += 1;
  slots.value.push(createSlot(`槽位${slotNumber}`));
  queueCalculate();
}

function createSlot(name: string): LocalSlot {
  return {
    id: `slot-${Date.now()}-${Math.random().toString(16).slice(2)}`,
    name,
    layout: 'ordered',
    leftTrunkingId: null,
    rightTrunkingId: null,
    leftFillRatio: null,
    rightFillRatio: null,
    pipes: [],
    sections: [
      { key: 'top', label: '上层', selectedTrunkingId: null, fillRatio: null, pipes: [] },
      { key: 'bottom', label: '下层', selectedTrunkingId: null, fillRatio: null, pipes: [] }
    ]
  };
}

function formatFillRatio(value: number | null | undefined) {
  return value == null ? '-' : `${(value * 100).toFixed(0)}%`;
}

function removeSlot(slotId: string) {
  slots.value = slots.value.filter(slot => slot.id !== slotId);
  queueCalculate();
}

function openPipePicker(slotId: string, sectionKey: 'top' | 'bottom') {
  pickerTarget.slotId = slotId;
  pickerTarget.sectionKey = sectionKey;
  showAddDialog.value = true;
}

function openSlotPipePicker(slotId: string) {
  openPipePicker(slotId, 'top');
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
  return slot?.sections.find(section => section.key === pickerTarget.sectionKey)?.pipes || null;
}

function createSlotRows(activePipes: ActivePipe[]): TrunkingSelectionRow[] {
  return createTrunkingSelectionRows(activePipes, pipeLib.value, trunkingModules.value, trunkingComponents.value);
}

type SegmentSelectionRow = TrunkingSelectionRow & {
  layerRef: OrderedSegmentLayerRef;
  layerLabel: string;
};

type SlotCombinedSelectionRow = TrunkingSelectionRow & {
  sectionKey: 'top' | 'bottom';
};

function getSegmentLayerRefs(segmentIndex: number) {
  return getOrderedSegmentLayerRefs(slots.value, segmentIndex);
}

function getLayerRefLabel(layerRef: OrderedSegmentLayerRef) {
  return `${layerRef.slotName}${layerRef.sectionKey === 'top' ? '上' : '下'}`;
}

function getHorizontalSegment(index: number) {
  return result.value?.slots[index] || null;
}

function getSideSlot(index: number) {
  return result.value?.sideSlots[index] || null;
}

function getSideSection(index: number, side: 'left' | 'right') {
  return getSideSlot(index)?.sections.find(section => section.key.endsWith(`-${side}`)) || null;
}

function getSideAreaText(index: number, side: 'left' | 'right') {
  return formatAreaText(getSideSection(index, side)?.totalArea || 0);
}

function getDetailTitle(item: SummaryItem) {
  if (item.slot.id.startsWith('segment-')) return `${item.slot.name}线槽`;
  return `${item.slot.name} / ${item.section.label}`;
}

function openSegmentDetail(segment: TrunkingSlotResult) {
  const section = segment.sections.find(item => item.selectedTrunking) || segment.sections[0];
  if (section) openDetail({ key: `${segment.id}-${section.key}`, slot: segment, section });
}

function createSegmentRows(segmentIndex: number): SegmentSelectionRow[] {
  return getSegmentLayerRefs(segmentIndex).flatMap(layerRef =>
    createSlotRows(layerRef.pipes).map(row => ({
      ...row,
      layerRef,
      layerLabel: getLayerRefLabel(layerRef)
    }))
  );
}

function createSlotCombinedRows(slot: LocalSlot): SlotCombinedSelectionRow[] {
  return slot.sections.flatMap(section =>
    createSlotRows(section.pipes).map(row => ({
      ...row,
      sectionKey: section.key
    }))
  );
}

function getSegmentAreaSummary(segmentIndex: number) {
  return summarizeTrunkingSelectionRows(createSegmentRows(segmentIndex));
}

function getSlotAreaSummary(slot: LocalSlot) {
  return summarizeTrunkingSelectionRows(createSlotCombinedRows(slot));
}

function getResultAreaText(segment: TrunkingSlotResult) {
  return formatAreaText(segment.sections.reduce((sum, section) => sum + section.totalArea, 0));
}

function getSegmentTrunkingSummary(segment: TrunkingSlotResult) {
  const selectedModels = segment.sections
    .map(section => section.selectedTrunking?.model)
    .filter(Boolean);
  if (selectedModels.length === 0) return '未计算线槽';
  return [...new Set(selectedModels)].join(' / ');
}

function updateSlotLayerPipeQty(slotId: string, sectionKey: 'top' | 'bottom', index: number, qty: number) {
  const section = slots.value
    .find(item => item.id === slotId)
    ?.sections.find(item => item.key === sectionKey);
  if (section) section.pipes[index] = { ...section.pipes[index], qty };
  queueCalculate();
}

function removeSlotLayerPipe(slotId: string, sectionKey: 'top' | 'bottom', index: number) {
  slots.value
    .find(item => item.id === slotId)
    ?.sections.find(item => item.key === sectionKey)
    ?.pipes.splice(index, 1);
  queueCalculate();
}

function toRequestSlots(): TrunkingSlotRequest[] {
  return slots.value
    .filter(slot => slot.name.trim())
    .map(slot => ({
      id: slot.id,
      name: slot.name.trim(),
      layout: 'ordered',
      pipes: [],
      sections: slot.sections.map(section => ({
        key: section.key,
        label: section.label,
        selectedTrunkingId: section.selectedTrunkingId,
        fillRatio: section.fillRatio,
        pipes: getExpandedSlotLayerPipes(slot, section.key)
      }))
    }));
}

function getExpandedSlotLayerPipes(slot: LocalSlot, layer: 'top' | 'bottom') {
  return slot.sections
    .flatMap(section => expandSelectionToPipes(
      section.pipes,
      trunkingModules.value,
      trunkingComponents.value,
      section.key
    ))
    .filter(pipe => pipe.layer === layer);
}

function formatAreaText(value: number) {
  if (value <= 0) return '0';
  const text = Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1);
  return text.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

async function calculate() {
  loading.value = true;
  error.value = '';
  try {
    result.value = await trunkingApi.calculate({
      selectedTrunkingId: 0,
      pipes: [],
      slots: toRequestSlots()
    });
  } catch (err) {
    error.value = err instanceof Error ? err.message : '计算失败';
  } finally {
    loading.value = false;
  }
}

async function refreshPipeSources() {
  await Promise.all([loadPipeLib(), loadPipeModules(), loadPipeComponents()]);
}

function loadWorkspace() {
  const runtimeState = getTrunkingRuntimeState();
  if (!runtimeState) return;

  slots.value = runtimeState.slots.map((slot, index) => ({
    ...createSlot(slot.name || `槽位${index + 1}`),
    ...slot,
    layout: 'ordered',
    pipes: [],
    sections: slot.layout === 'ordered'
      ? slot.sections
      : slot.layout === 'topBottom'
      ? slot.sections
      : [
          { key: 'top', label: '上层', selectedTrunkingId: null, fillRatio: null, pipes: slot.pipes },
          { key: 'bottom', label: '下层', selectedTrunkingId: null, fillRatio: null, pipes: [] }
        ]
  }));
  nextSlotNumber.value = Math.max(runtimeState.nextSlotNumber || 1, getNextSlotNumber(slots.value));
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
    slots: slots.value,
    nextSlotNumber: nextSlotNumber.value
  };
}

function getNextSlotNumber(items: LocalSlot[]) {
  const maxSlotNumber = items.reduce((max, slot) => {
    const match = /^槽位(\d+)$/.exec(slot.name.trim());
    return match ? Math.max(max, Number(match[1])) : max;
  }, 0);
  return maxSlotNumber + 1;
}

function openDetail(item: SummaryItem) {
  detailItem.value = item;
  detailVisible.value = true;
}

function openPipeDetail(row: TrunkingSelectionRow) {
  pipeDetailItem.value = row;
  pipeDetailVisible.value = true;
}

function isPipeSelection(pipe: ActivePipe): pipe is Extract<ActivePipe, { kind?: 'pipe' }> {
  return !pipe.kind || pipe.kind === 'pipe';
}

let calculateTimer = 0;
let saveWorkspaceStatusTimer = 0;

function queueCalculate() {
  window.clearTimeout(calculateTimer);
  calculateTimer = window.setTimeout(calculate, 300);
}

watch(slots, () => {
  queueCalculate();
  saveWorkspace();
}, { deep: true });

onMounted(async () => {
  await Promise.all([trunkingApi.getAll(), refreshPipeSources()]);
  loadWorkspace();
  await calculate();
  mounted.value = true;
});

onActivated(async () => {
  if (!mounted.value) return;
  await refreshPipeSources();
  await calculate();
});
</script>

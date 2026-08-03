<template>
  <PageShell>
    <div class="trunking-layout">
      <section class="trunking-slot-board">
        <el-card shadow="never" class="control-card">
          <template #header>
            <div class="card-header-row">
              <span class="selection-title">
                <span>选型参数</span>
                <el-tag size="small" :type="loadedSavedId ? 'primary' : 'info'">
                  {{ currentSelectionTitle }}
                </el-tag>
              </span>
              <span class="card-header-actions">
                <el-button size="small" @click="newSelection">
                  新建选型
                </el-button>
                <el-button size="small" :loading="selectionSaving" :disabled="!slots.length" @click="openSaveSelectionDialog">
                  保存选型
                </el-button>
                <el-button size="small" type="primary" @click="addSlot">
                  添加槽位
                </el-button>
              </span>
            </div>
          </template>
        </el-card>

        <el-scrollbar class="slot-scroll">
          <el-empty v-if="!slots.length" description="请先添加槽位" />
          <div v-else class="trunking-stack">
            <el-alert v-if="error" :title="error" type="error" show-icon :closable="false" class="summary-error" />

            <div class="vertical-slot-layout">
              <button
                v-if="getVerticalSideSection('left')"
                type="button"
                class="segment-side is-vertical"
                :class="getVerticalSideSection('left')?.resultStatus"
                @click="openVerticalSideDetail('left')"
              >
                <span>左</span>
                <strong>{{ getVerticalSideSection('left')?.selectedTrunking?.model || '-' }}</strong>
                <small>{{ getVerticalSideSection('left')?.selectedTrunking ? `${(getVerticalSideSection('left')!.actualFillRatio * 100).toFixed(1)}%` : getVerticalSideSection('left')?.resultMessage }}</small>
                <em>弱电面积 {{ getVerticalSideAreaText('left') }} mm²</em>
              </button>

              <div class="trunking-center-stack">
                <template v-for="(slot, index) in displaySlots" :key="slot.id">
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
                          <span class="area-summary is-muted">{{ getSlotPipeCount(slot) }} 项</span>
                          <el-button size="small" text @click="toggleSlotPipeList(slot.id)">
                            {{ isSlotPipeListExpanded(slot.id) ? '收起' : '展开' }}
                          </el-button>
                          <el-button size="small" :loading="pipeSourceRefreshing" @click="openSlotPipePicker(slot.id)">添加管线</el-button>
                        </div>
                        <el-table v-if="isSlotPipeListExpanded(slot.id)" :data="createSlotCombinedRows(slot)" size="small" border max-height="260" empty-text="暂无管线">
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

              <button
                v-if="getVerticalSideSection('right')"
                type="button"
                class="segment-side is-vertical"
                :class="getVerticalSideSection('right')?.resultStatus"
                @click="openVerticalSideDetail('right')"
              >
                <span>右</span>
                <strong>{{ getVerticalSideSection('right')?.selectedTrunking?.model || '-' }}</strong>
                <small>{{ getVerticalSideSection('right')?.selectedTrunking ? `${(getVerticalSideSection('right')!.actualFillRatio * 100).toFixed(1)}%` : getVerticalSideSection('right')?.resultMessage }}</small>
                <em>强电面积 {{ getVerticalSideAreaText('right') }} mm²</em>
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

    <el-dialog v-model="saveDialogVisible" title="保存选型" width="min(420px, calc(100vw - 24px))">
      <el-form label-width="84px" @submit.prevent>
        <el-form-item label="选型名称" required>
          <el-input
            v-model="saveSelectionName"
            maxlength="60"
            show-word-limit
            placeholder="请输入保存名称"
            @keyup.enter="saveCurrentSelection"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="saveDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="selectionSaving" @click="saveCurrentSelection">保存</el-button>
      </template>
    </el-dialog>
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import AddPipeDialog from '../components/AddPipeDialog.vue';
import MetricItem from '../widgets/MetricItem.vue';
import PageShell from '../components/PageShell.vue';
import { trunkingApi } from '../api/trunking';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeComponents } from '../composables/usePipeComponents';
import { usePipeModules } from '../composables/usePipeModules';
import type { ActivePipe, PipeType, TrunkingCalcRequest, TrunkingCalcResponse, TrunkingSavedSelection, TrunkingSideResult, TrunkingSlotRequest, TrunkingSlotResult } from '../types';
import { expandSelectionToPipes } from '../utils/pipeSelection';
import { getPipeDisplayType } from '../utils/pipeType';
import { createTrunkingSelectionRows, summarizeTrunkingSelectionRows, type TrunkingSelectionDetailRow, type TrunkingSelectionRow } from '../utils/trunkingSelectionDisplay';
import { getSlotsTopToBottom, renumberDefaultSlots } from '../utils/trunkingSlotOrdering';
import { getTrunkingRuntimeState, setTrunkingRuntimeState, type LocalSlot } from '../stores/trunkingRuntimeState';
import { getOrderedSegmentLayerRefs, type OrderedSegmentLayerRef } from '../utils/trunkingSegmentLayers';
import { getRequestSlotsBottomToTop } from '../utils/trunkingSavedSelectionSlots';

interface SummaryItem {
  key: string;
  slot: TrunkingSlotResult;
  section: TrunkingSideResult;
}

const TRUNKING_ALLOWED_TYPES = ['weak_cable', 'strong_cable', 'encoder'];

const { pipeLib, loadPipeLib } = usePipeLibrary();
const { pipeModules, loadPipeModules } = usePipeModules();
const { pipeComponents, loadPipeComponents } = usePipeComponents();
const route = useRoute();
const router = useRouter();
const slots = ref<LocalSlot[]>([]);
const result = ref<TrunkingCalcResponse | null>(null);
const loading = ref(false);
const error = ref('');
const workspaceSaving = ref(false);
const workspaceSaved = ref(false);
const showAddDialog = ref(false);
const pipeSourceRefreshing = ref(false);
const selectionSaving = ref(false);
const nextSlotNumber = ref(1);
const pickerTarget = reactive<{ slotId: string; sectionKey: 'top' | 'bottom' }>({ slotId: '', sectionKey: 'top' });
const detailVisible = ref(false);
const detailItem = ref<SummaryItem | null>(null);
const mounted = ref(false);
const pipeDetailVisible = ref(false);
const pipeDetailItem = ref<TrunkingSelectionRow | null>(null);
const expandedSlotIds = ref<Set<string>>(new Set());
const loadedSavedId = ref('');
const loadedSavedName = ref('');
const saveDialogVisible = ref(false);
const saveSelectionName = ref('线槽选型');
const currentSelectionTitle = computed(() => loadedSavedName.value || '新建选型');
const displaySlots = computed(() => getSlotsTopToBottom(slots.value));

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

function newSelection() {
  loadedSavedId.value = '';
  loadedSavedName.value = '';
  saveSelectionName.value = '线槽选型';
  expandedSlotIds.value = new Set();
  nextSlotNumber.value = 2;
  slots.value = [createSlot('槽位1')];
  result.value = null;
  error.value = '';
  saveWorkspace();
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
  slots.value = renumberDefaultSlots(slots.value.filter(slot => slot.id !== slotId));
  const nextExpanded = new Set(expandedSlotIds.value);
  nextExpanded.delete(slotId);
  expandedSlotIds.value = nextExpanded;
  nextSlotNumber.value = getNextSlotNumber(slots.value);
  queueCalculate();
}

async function openPipePicker(slotId: string, sectionKey: 'top' | 'bottom') {
  pickerTarget.slotId = slotId;
  pickerTarget.sectionKey = sectionKey;
  expandSlotPipeList(slotId);
  if (!await refreshPipeSourcesForPicker()) return;
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
  return getOrderedSegmentLayerRefs(displaySlots.value, segmentIndex);
}

function getLayerRefLabel(layerRef: OrderedSegmentLayerRef) {
  return `${layerRef.slotName}${layerRef.sectionKey === 'top' ? '上' : '下'}`;
}

function getHorizontalSegment(index: number) {
  return result.value?.slots[index] || null;
}

function getVerticalSideSlot() {
  return result.value?.sideSlots[0] || null;
}

function getVerticalSideSection(side: 'left' | 'right') {
  return getVerticalSideSlot()?.sections.find((section: TrunkingSideResult) => section.key.endsWith(`-${side}`)) || null;
}

function getVerticalSideAreaText(side: 'left' | 'right') {
  return formatAreaText(getVerticalSideSection(side)?.totalArea || 0);
}

function openVerticalSideDetail(side: 'left' | 'right') {
  const slot = getVerticalSideSlot();
  const section = getVerticalSideSection(side);
  if (!slot || !section) return;
  openDetail({ key: `${slot.id}-${side}`, slot, section });
}

function getDetailTitle(item: SummaryItem) {
  if (item.slot.id.startsWith('segment-')) return `${item.slot.name}线槽`;
  return `${item.slot.name} / ${item.section.label}`;
}

function openSegmentDetail(segment: TrunkingSlotResult) {
  const section = segment.sections.find((item: TrunkingSideResult) => item.selectedTrunking) || segment.sections[0];
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

function isSlotPipeListExpanded(slotId: string) {
  return expandedSlotIds.value.has(slotId);
}

function expandSlotPipeList(slotId: string) {
  expandedSlotIds.value = new Set([...expandedSlotIds.value, slotId]);
}

function toggleSlotPipeList(slotId: string) {
  const next = new Set(expandedSlotIds.value);
  if (next.has(slotId)) next.delete(slotId);
  else next.add(slotId);
  expandedSlotIds.value = next;
}

function getSlotPipeCount(slot: LocalSlot) {
  return createSlotCombinedRows(slot).length;
}

function getSegmentAreaSummary(segmentIndex: number) {
  return summarizeTrunkingSelectionRows(createSegmentRows(segmentIndex));
}

function getSlotAreaSummary(slot: LocalSlot) {
  return summarizeTrunkingSelectionRows(createSlotCombinedRows(slot));
}

function getResultAreaText(segment: TrunkingSlotResult) {
  return formatAreaText(segment.sections.reduce((sum: number, section: TrunkingSideResult) => sum + section.totalArea, 0));
}

function getSegmentTrunkingSummary(segment: TrunkingSlotResult) {
  const selectedModels = segment.sections
    .map((section: TrunkingSideResult) => section.selectedTrunking?.model)
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
    result.value = await trunkingApi.calculate(createCalcRequest());
  } catch (err) {
    error.value = err instanceof Error ? err.message : '计算失败';
  } finally {
    loading.value = false;
  }
}

function createCalcRequest(): TrunkingCalcRequest {
  return {
    selectedTrunkingId: 0,
    pipes: [],
    slotOrder: 'bottomToTop',
    slots: toRequestSlots()
  };
}

function createSavedSourceSlots() {
  return slots.value.map(slot => ({
    id: slot.id,
    name: slot.name,
    sections: slot.sections.map(section => ({
      key: section.key,
      label: section.label,
      pipes: section.pipes
    }))
  }));
}

function openSaveSelectionDialog() {
  saveSelectionName.value = loadedSavedName.value || getDefaultSelectionName();
  saveDialogVisible.value = true;
}

function getDefaultSelectionName() {
  const firstSlotName = slots.value[0]?.name?.trim();
  return firstSlotName ? `${firstSlotName}线槽选型` : '线槽选型';
}

async function saveCurrentSelection() {
  if (!slots.value.length) {
    ElMessage.warning('请先添加槽位');
    return;
  }

  const selectionName = saveSelectionName.value.trim();
  if (!selectionName) {
    ElMessage.warning('请输入选型名称');
    return;
  }

  selectionSaving.value = true;
  try {
    const saved = await trunkingApi.saveSelection({
      id: loadedSavedId.value || undefined,
      name: selectionName,
      request: createCalcRequest(),
      result: result.value,
      sourceSlots: createSavedSourceSlots()
    });
    loadedSavedId.value = saved.id || '';
    loadedSavedName.value = saved.name || selectionName;
    result.value = saved.result;
    saveDialogVisible.value = false;
    ElMessage.success('选型已保存到数据库');
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '保存失败');
  } finally {
    selectionSaving.value = false;
  }
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
    error.value = err instanceof Error ? err.message : '管线库加载失败';
    return false;
  } finally {
    pipeSourceRefreshing.value = false;
  }
}

function loadWorkspace() {
  const runtimeState = getTrunkingRuntimeState();
  if (!runtimeState) return;

  applySlots(runtimeState.slots);
  nextSlotNumber.value = Math.max(runtimeState.nextSlotNumber || 1, getNextSlotNumber(slots.value));
}

async function loadSavedSelection() {
  const savedId = typeof route.query.savedId === 'string' ? route.query.savedId : '';
  const saved = savedId
    ? await trunkingApi.getSavedSelectionById(savedId)
    : await trunkingApi.getSavedSelection();
  if (!saved?.request?.slots?.length) return false;

  applySavedSelection(saved);
  loadedSavedId.value = saved.id || '';
  loadedSavedName.value = saved.name || '';
  return true;
}

function applySavedSelection(saved: TrunkingSavedSelection) {
  applySlots(getRequestSlotsBottomToTop(saved.request).map(slot => ({
    id: slot.id,
    name: slot.name,
    layout: slot.layout,
    leftTrunkingId: slot.leftTrunkingId ?? null,
    rightTrunkingId: slot.rightTrunkingId ?? null,
    leftFillRatio: slot.leftFillRatio ?? null,
    rightFillRatio: slot.rightFillRatio ?? null,
    pipes: [],
    sections: (slot.sections || []).map(section => ({
      key: section.key === 'bottom' ? 'bottom' : 'top',
      label: section.label,
      selectedTrunkingId: section.selectedTrunkingId ?? null,
      fillRatio: section.fillRatio ?? null,
      pipes: section.pipes.map(pipe => ({ kind: 'pipe' as const, libId: pipe.pipeTypeId, qty: pipe.qty }))
    }))
  })));
  nextSlotNumber.value = getNextSlotNumber(slots.value);
  result.value = saved.result;
}

function applySlots(items: Array<Partial<LocalSlot> & { name: string }>) {
  slots.value = items.map((slot, index) => {
    const baseSlot = createSlot(slot.name || `槽位${index + 1}`);
    const sections = slot.layout === 'ordered' || slot.layout === 'topBottom'
      ? slot.sections || baseSlot.sections
      : [
          { key: 'top' as const, label: '上层', selectedTrunkingId: null, fillRatio: null, pipes: slot.pipes || [] },
          { key: 'bottom' as const, label: '下层', selectedTrunkingId: null, fillRatio: null, pipes: [] }
        ];

    return {
      ...baseSlot,
      ...slot,
      layout: 'ordered',
      pipes: [],
      sections
    };
  });
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
  if (!await loadSavedSelection()) {
    loadWorkspace();
  }
  await calculate();
  mounted.value = true;
});

onActivated(async () => {
  if (!mounted.value) return;
  await refreshPipeSources();
  const savedId = typeof route.query.savedId === 'string' ? route.query.savedId : '';
  if (savedId && savedId !== loadedSavedId.value) {
    await loadSavedSelection();
    await router.replace({ path: route.path, query: {} });
  }
  await calculate();
});
</script>

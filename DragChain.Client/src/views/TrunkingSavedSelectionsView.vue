<template>
  <PageShell>
    <div class="saved-selection-page">
      <section class="saved-selection-list">
        <div class="saved-selection-toolbar">
          <el-input v-model="keyword" clearable placeholder="搜索保存名称" />
          <el-button :loading="loading" @click="loadSelections">刷新</el-button>
        </div>

        <el-scrollbar class="saved-selection-scroll">
          <el-empty v-if="!filteredSelections.length" description="暂无保存选型" />
          <button
            v-for="item in filteredSelections"
            v-else
            :key="item.id || item.savedAt"
            type="button"
            class="saved-selection-item"
            :class="{ 'is-active': selected?.id === item.id }"
            @click="selectSelection(item)"
          >
            <span>
              <strong>{{ item.name || '线槽选型' }}</strong>
              <small>{{ formatTime(item.savedAt) }}</small>
            </span>
            <el-tag :type="getStatusType(item.result?.resultStatus)" size="small">
              {{ getStatusText(item.result?.resultStatus) }}
            </el-tag>
          </button>
        </el-scrollbar>
      </section>

      <section class="saved-selection-detail">
        <el-empty v-if="!selected" description="请选择保存的选型" />
        <template v-else>
          <div class="saved-detail-head">
            <div>
              <h2>{{ selected.name || '线槽选型' }}</h2>
              <p>{{ formatTime(selected.savedAt) }}</p>
            </div>
            <div class="saved-detail-actions">
              <el-button type="primary" @click="openSelection">载入到选型页</el-button>
              <el-button type="danger" plain :loading="deleting" @click="deleteSelection">删除</el-button>
            </div>
          </div>

          <div class="trunking-stack saved-trunking-stack">
            <div class="vertical-slot-layout">
              <button
                v-if="getVerticalSideSection('left')"
                type="button"
                class="segment-side is-vertical"
                :class="getVerticalSideSection('left')?.resultStatus"
              >
                <span>左</span>
                <strong>{{ getVerticalSideSection('left')?.selectedTrunking?.model || '-' }}</strong>
                <small>{{ getVerticalSideSection('left')?.selectedTrunking ? formatPercent(getVerticalSideSection('left')?.actualFillRatio) : getVerticalSideSection('left')?.resultMessage }}</small>
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
                    >
                      <strong>{{ getHorizontalSegment(index)?.name }}</strong>
                      <small>{{ getSegmentTrunkingSummary(getHorizontalSegment(index)!) }}</small>
                      <span class="segment-inline-actions">
                        <span>管线面积 {{ getResultAreaText(getHorizontalSegment(index)!) }} mm²</span>
                      </span>
                    </button>
                  </div>

                  <div class="slot-layout-row">
                    <el-card shadow="never" class="slot-card saved-slot-card">
                      <template #header>
                        <div class="slot-header">
                          <strong>{{ slot.name }}</strong>
                          <span class="area-summary is-muted">{{ getSlotTotalCount(slot) }} 项</span>
                        </div>
                      </template>
                      <div class="slot-section">
                        <div class="section-title">
                          <span>管线清单</span>
                        </div>
                        <el-table :data="getSlotPipeRows(slot)" size="small" border max-height="260" empty-text="暂无管线">
                          <el-table-column prop="name" label="管线 / 模块 / 元件" min-width="190" show-overflow-tooltip />
                          <el-table-column prop="kindLabel" label="来源" width="76" align="center" />
                          <el-table-column prop="layerLabel" label="上下" width="64" align="center" />
                          <el-table-column prop="qtyText" label="数量" width="78" align="center" />
                          <el-table-column prop="sideLabel" label="分侧" width="78" align="center" />
                          <el-table-column prop="areaText" label="面积" width="92" align="right" />
                        </el-table>
                      </div>
                    </el-card>
                  </div>
                </template>

                <div class="trunking-stack-row is-segment">
                  <button
                    v-if="getHorizontalSegment(displaySlots.length)"
                    type="button"
                    class="segment-name"
                    :class="getHorizontalSegment(displaySlots.length)?.resultStatus"
                  >
                    <strong>{{ getHorizontalSegment(displaySlots.length)?.name }}</strong>
                    <small>{{ getSegmentTrunkingSummary(getHorizontalSegment(displaySlots.length)!) }}</small>
                    <span class="segment-inline-actions">
                      <span>管线面积 {{ getResultAreaText(getHorizontalSegment(displaySlots.length)!) }} mm²</span>
                    </span>
                  </button>
                </div>
              </div>

              <button
                v-if="getVerticalSideSection('right')"
                type="button"
                class="segment-side is-vertical"
                :class="getVerticalSideSection('right')?.resultStatus"
              >
                <span>右</span>
                <strong>{{ getVerticalSideSection('right')?.selectedTrunking?.model || '-' }}</strong>
                <small>{{ getVerticalSideSection('right')?.selectedTrunking ? formatPercent(getVerticalSideSection('right')?.actualFillRatio) : getVerticalSideSection('right')?.resultMessage }}</small>
                <em>强电面积 {{ getVerticalSideAreaText('right') }} mm²</em>
              </button>
            </div>
          </div>
        </template>
      </section>
    </div>
  </PageShell>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import PageShell from '../components/PageShell.vue';
import MetricItem from '../widgets/MetricItem.vue';
import { trunkingApi } from '../api/trunking';
import { usePipeLibrary } from '../composables/usePipeLibrary';
import { usePipeComponents } from '../composables/usePipeComponents';
import { usePipeModules } from '../composables/usePipeModules';
import { createTrunkingSelectionRows } from '../utils/trunkingSelectionDisplay';
import type { ActivePipe, TrunkingSavedSelection, TrunkingSavedSourceSlot, TrunkingSlotResult, TrunkingSideResult } from '../types';
import { getSavedSelectionSlotsTopToBottom } from '../utils/trunkingSavedSelectionSlots';

const router = useRouter();
const { pipeLib, loadPipeLib } = usePipeLibrary();
const { pipeModules, loadPipeModules } = usePipeModules();
const { pipeComponents, loadPipeComponents } = usePipeComponents();
const loading = ref(false);
const deleting = ref(false);
const keyword = ref('');
const selections = ref<TrunkingSavedSelection[]>([]);
const selected = ref<TrunkingSavedSelection | null>(null);

const filteredSelections = computed(() => {
  const text = keyword.value.trim().toLowerCase();
  if (!text) return selections.value;
  return selections.value.filter(item => (item.name || '线槽选型').toLowerCase().includes(text));
});

const displaySlots = computed(() => getSavedSelectionSlotsTopToBottom(selected.value));

const pipeRows = computed(() =>
  displaySlots.value.flatMap(slot =>
    slot.sections.flatMap(section =>
      createTrunkingSelectionRows(section.pipes, pipeLib.value, pipeModules.value, pipeComponents.value)
        .map(row => ({
          slotId: slot.id,
          slotName: slot.name,
          layerLabel: section.key === 'bottom' ? '下' : '上',
          kindLabel: row.kind === 'module' ? '模块' : row.kind === 'component' ? '元件' : '管线',
          name: row.name,
          detail: row.detail === '-' ? '' : row.detail,
          qtyText: String(row.qty),
          sideLabel: row.sideLabel,
          sizeText: row.sizeText,
          areaText: row.areaText
        }))
    )
  )
);

onMounted(loadSelections);
onActivated(loadSelections);

async function loadSelections() {
  loading.value = true;
  try {
    const selectedId = selected.value?.id || '';
    await Promise.all([loadPipeLib(), loadPipeModules(), loadPipeComponents()]);
    selections.value = await trunkingApi.getSavedSelections();
    selected.value = selections.value.find(item => item.id === selectedId) || selections.value[0] || null;
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '加载保存选型失败');
  } finally {
    loading.value = false;
  }
}

function selectSelection(item: TrunkingSavedSelection) {
  selected.value = item;
}

async function openSelection() {
  if (!selected.value?.id) return;
  await router.push({ path: '/trunking/calc', query: { savedId: selected.value.id } });
}

async function deleteSelection() {
  if (!selected.value?.id) return;
  const selection = selected.value;
  const selectionId: string = selected.value.id;
  try {
    await ElMessageBox.confirm(
      `确认删除「${selection.name || '线槽选型'}」？删除后无法从保存列表恢复。`,
      '二次确认',
      {
        type: 'warning',
        confirmButtonText: '确认删除',
        cancelButtonText: '取消',
        confirmButtonClass: 'el-button--danger'
      }
    );
  } catch {
    return;
  }

  deleting.value = true;
  try {
    await trunkingApi.deleteSavedSelection(selectionId);
    ElMessage.success('已删除保存选型');
    await loadSelections();
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '删除失败');
  } finally {
    deleting.value = false;
  }
}

function countActivePipes(pipes: ActivePipe[]) {
  return pipes.reduce((sum, pipe) => sum + Number(pipe.qty || 0), 0);
}

function getVerticalSideSlot() {
  return selected.value?.result?.sideSlots?.[0] || null;
}

function getVerticalSideSection(side: 'left' | 'right') {
  return getVerticalSideSlot()?.sections.find(section => section.key.endsWith(`-${side}`)) || null;
}

function getVerticalSideAreaText(side: 'left' | 'right') {
  return formatArea(getVerticalSideSection(side)?.totalArea || 0);
}

function getHorizontalSegment(index: number) {
  return selected.value?.result?.slots?.[index] || null;
}

function getSegmentTrunkingSummary(segment: TrunkingSlotResult) {
  const selectedModels = segment.sections
    .map((section: TrunkingSideResult) => section.selectedTrunking?.model)
    .filter(Boolean);
  if (selectedModels.length === 0) return '未计算线槽';
  return [...new Set(selectedModels)].join(' / ');
}

function getResultAreaText(segment: TrunkingSlotResult) {
  return formatArea(segment.sections.reduce((sum: number, section: TrunkingSideResult) => sum + section.totalArea, 0));
}

function getSlotTotalCount(slot: TrunkingSavedSourceSlot) {
  return slot.sections.reduce((sum, section) => sum + countActivePipes(section.pipes), 0);
}

function getSlotPipeRows(slot: TrunkingSavedSourceSlot) {
  return pipeRows.value.filter(row => row.slotId === slot.id);
}

function formatTime(value?: string) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString('zh-CN', { hour12: false });
}

function formatArea(value: number) {
  if (!value) return '0';
  return value.toLocaleString('zh-CN', { maximumFractionDigits: 1 });
}

function formatPercent(value?: number) {
  return value == null ? '-' : `${(value * 100).toFixed(1)}%`;
}

function getStatusText(status?: string) {
  if (status === 'ok') return '可用';
  if (status === 'warn') return '警告';
  if (status === 'err') return '异常';
  return '未计算';
}

function getStatusType(status?: string) {
  if (status === 'ok') return 'success';
  if (status === 'warn') return 'warning';
  if (status === 'err') return 'danger';
  return 'info';
}
</script>

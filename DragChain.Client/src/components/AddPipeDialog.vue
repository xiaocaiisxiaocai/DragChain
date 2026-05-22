<template>
  <el-dialog
    v-model="visible"
    title="从管线库新增"
    width="560px"
    class="add-pipe-dialog"
    destroy-on-close
  >
    <el-alert
      title="已在当前清单中的管线、模块或元件会自动禁用"
      type="info"
      :closable="false"
      show-icon
      class="dialog-tip"
    />

    <el-tabs v-model="activeTab" class="pipe-picker-tabs">
      <el-tab-pane label="管线" name="pipes">
        <el-checkbox-group v-model="selectedPipeIds">
          <section v-for="group in visibleGroups" :key="group.key" class="pipe-group">
            <div class="pipe-group-title">{{ group.label }}</div>
            <el-checkbox
              v-for="pipe in groupedPipes[group.key]"
              :key="pipe.id"
              :value="pipe.id"
              :disabled="activePipeIdSet.has(pipe.id)"
              border
            >
              <span class="pipe-option-name">{{ pipe.name }}</span>
              <span class="pipe-option-meta">Φ{{ pipe.diameter }} · {{ pipe.weight }}kg/m</span>
            </el-checkbox>
          </section>
        </el-checkbox-group>
      </el-tab-pane>

      <el-tab-pane label="模块" name="modules">
        <el-checkbox-group v-model="selectedModuleIds">
          <section class="pipe-group">
            <el-empty v-if="!pipeModules.length" description="暂无模块" />
            <el-checkbox
              v-for="module in pipeModules"
              :key="module.id"
              :value="module.id"
              :disabled="activeModuleIdSet.has(module.id)"
              border
            >
              <span class="pipe-option-name">{{ module.name }}</span>
              <span class="pipe-option-meta">{{ describeGroup(module) }}</span>
            </el-checkbox>
          </section>
        </el-checkbox-group>
      </el-tab-pane>

      <el-tab-pane label="元件" name="components">
        <el-checkbox-group v-model="selectedComponentIds">
          <section class="pipe-group">
            <el-empty v-if="!pipeComponents.length" description="暂无元件" />
            <el-checkbox
              v-for="component in pipeComponents"
              :key="component.id"
              :value="component.id"
              :disabled="activeComponentIdSet.has(component.id)"
              border
            >
              <span class="pipe-option-name">{{ component.name }}</span>
              <span class="pipe-option-meta">{{ describeGroup(component) }}</span>
            </el-checkbox>
          </section>
        </el-checkbox-group>
      </el-tab-pane>
    </el-tabs>

    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" @click="confirm">加入选中</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { ActivePipe, PipeComponent, PipeModule, PipeType } from '../types';
import { getPipeDisplayLabel, getPipeDisplayType } from '../utils/pipeType';

const props = defineProps<{
  pipeLib: PipeType[];
  pipeModules: PipeModule[];
  pipeComponents?: PipeComponent[];
  activePipes: ActivePipe[];
  allowedTypes?: string[];
  allowDuplicates?: boolean;
}>();

const emit = defineEmits<{
  confirm: [payload: { pipeIds: number[]; moduleIds: number[]; componentIds: number[] }];
}>();

const visible = defineModel<boolean>({ required: true });
const activeTab = ref('pipes');
const selectedPipeIds = ref<number[]>([]);
const selectedModuleIds = ref<number[]>([]);
const selectedComponentIds = ref<number[]>([]);

const groups = [
  { key: 'tube', label: '气管' },
  { key: 'weak_cable', label: '弱电电缆' },
  { key: 'strong_cable', label: '强电电缆' },
  { key: 'encoder', label: '编码器线' },
  { key: 'other', label: '其他' }
];

const activePipeIdSet = computed(() =>
  props.allowDuplicates ? new Set<number>() : new Set(props.activePipes.filter(isPipeSelection).map(pipe => pipe.libId))
);

const activeModuleIdSet = computed(() =>
  props.allowDuplicates ? new Set<number>() : new Set(props.activePipes.filter(pipe => pipe.kind === 'module').map(pipe => pipe.moduleId))
);

const activeComponentIdSet = computed(() =>
  props.allowDuplicates ? new Set<number>() : new Set(props.activePipes.filter(pipe => pipe.kind === 'component').map(pipe => pipe.componentId))
);

const pipeComponents = computed(() => props.pipeComponents || []);

const groupedPipes = computed(() =>
  groups.reduce(
    (acc, group) => {
      acc[group.key] = props.pipeLib.filter(pipe => {
        if (props.allowedTypes && !props.allowedTypes.includes(getPipeDisplayType(pipe))) return false;
        return getPipeDisplayType(pipe) === group.key;
      });
      return acc;
    },
    {} as Record<string, PipeType[]>
  )
);

const visibleGroups = computed(() => groups.filter(group => groupedPipes.value[group.key].length > 0));

function describeGroup(group: PipeModule | PipeComponent) {
  return group.items
    .map(item => {
      const pipe = item.pipeType || props.pipeLib.find(pipeItem => pipeItem.id === item.pipeTypeId);
      const name = pipe ? `${pipe.name}(${getPipeDisplayLabel(pipe)})` : `#${item.pipeTypeId}`;
      return `${name}×${item.qty}`;
    })
    .join('，');
}

function isPipeSelection(pipe: ActivePipe): pipe is Extract<ActivePipe, { kind?: 'pipe' }> {
  return !pipe.kind || pipe.kind === 'pipe';
}

function confirm() {
  emit('confirm', {
    pipeIds: selectedPipeIds.value,
    moduleIds: selectedModuleIds.value,
    componentIds: selectedComponentIds.value
  });
  selectedPipeIds.value = [];
  selectedModuleIds.value = [];
  selectedComponentIds.value = [];
  visible.value = false;
}
</script>

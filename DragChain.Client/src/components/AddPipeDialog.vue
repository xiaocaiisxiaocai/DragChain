<template>
  <el-dialog
    v-model="visible"
    title="从管线库新增"
    width="560px"
    destroy-on-close
  >
    <el-alert
      title="已在当前清单中的管线或模块会自动禁用"
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
              <span class="pipe-option-meta">{{ describeModule(module) }}</span>
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
import type { ActivePipe, PipeModule, PipeType } from '../types';
import { getPipeDisplayLabel, getPipeDisplayType } from '../utils/pipeType';

const props = defineProps<{
  pipeLib: PipeType[];
  pipeModules: PipeModule[];
  activePipes: ActivePipe[];
  allowedTypes?: string[];
}>();

const emit = defineEmits<{
  confirm: [payload: { pipeIds: number[]; moduleIds: number[] }];
}>();

const visible = defineModel<boolean>({ required: true });
const activeTab = ref('pipes');
const selectedPipeIds = ref<number[]>([]);
const selectedModuleIds = ref<number[]>([]);

const groups = [
  { key: 'tube', label: '气管' },
  { key: 'weak_cable', label: '弱电电缆' },
  { key: 'strong_cable', label: '强电电缆' },
  { key: 'encoder', label: '编码器线' },
  { key: 'other', label: '其他' }
];

const activePipeIdSet = computed(() =>
  new Set(props.activePipes.filter(pipe => pipe.kind !== 'module').map(pipe => pipe.libId))
);

const activeModuleIdSet = computed(() =>
  new Set(props.activePipes.filter(pipe => pipe.kind === 'module').map(pipe => pipe.moduleId))
);

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

function describeModule(module: PipeModule) {
  return module.items
    .map(item => {
      const pipe = item.pipeType || props.pipeLib.find(pipeItem => pipeItem.id === item.pipeTypeId);
      const name = pipe ? `${pipe.name}(${getPipeDisplayLabel(pipe)})` : `#${item.pipeTypeId}`;
      return `${name}×${item.qty}`;
    })
    .join('，');
}

function confirm() {
  emit('confirm', {
    pipeIds: selectedPipeIds.value,
    moduleIds: selectedModuleIds.value
  });
  selectedPipeIds.value = [];
  selectedModuleIds.value = [];
  visible.value = false;
}
</script>

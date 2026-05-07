<template>
  <EditableCatalogTable
    title="犸幕 ME 型录"
    :columns="columns"
    :get-rows="meApi.getAll"
    :create-row="row => meApi.create(row as unknown as CreateMeCatalog)"
    :update-row="(id, row) => meApi.update(id, row as unknown as CreateMeCatalog)"
    :delete-row="meApi.delete"
  />
</template>

<script setup lang="ts">
import EditableCatalogTable from '../components/EditableCatalogTable.vue';
import { meApi, type CreateMeCatalog } from '../api/catalog';
import type { CatalogColumn } from '../types/catalogTable';

const columns: CatalogColumn[] = [
  { prop: 'baseModel', label: '犸幕', type: 'text', width: 140 },
  { prop: 'functionSelect', label: '功能选择', type: 'text', width: 120 },
  { prop: 'innerHeight', label: '拖管内高A(mm)', width: 130 },
  { prop: 'innerWidth', label: '拖管内宽B(mm)', width: 130 },
  { prop: 'r1', label: 'R1', width: 80 },
  { prop: 'r2', label: 'R2', width: 80 },
  { prop: 'r3', label: 'R3', width: 80 },
  { prop: 'r1Suffix', label: 'R1 后缀', type: 'text', width: 100 },
  { prop: 'r2Suffix', label: 'R2 后缀', type: 'text', width: 100 },
  { prop: 'r3Suffix', label: 'R3 后缀', type: 'text', width: 100 },
  { prop: 'lp1', label: 'LP1', width: 90 },
  { prop: 'lp2', label: 'LP2', width: 90 },
  { prop: 'lp3', label: 'LP3', width: 90 },
  { prop: 'mountingH1', label: '安装高度H1(mm)', type: 'text', width: 140 },
  { prop: 'innerArea', label: '内部面积', width: 110 },
  { prop: 'maxWeight', label: '最大重量', width: 110, precision: 3, step: 0.01 },
  { prop: 'spanBase', label: '架空基准', width: 110 },
  { prop: 'spanSlope', label: '架空斜率', width: 110, precision: 4, step: 0.0001 }
];
</script>

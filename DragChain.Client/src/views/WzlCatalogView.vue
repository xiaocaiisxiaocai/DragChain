<template>
  <EditableCatalogTable
    title="沃德 WZL 型录"
    :columns="columns"
    :get-rows="wzlApi.getAll"
    :create-row="row => wzlApi.create(row as unknown as CreateWzlCatalog)"
    :update-row="(id, row) => wzlApi.update(id, row as unknown as CreateWzlCatalog)"
    :delete-row="wzlApi.delete"
  />
</template>

<script setup lang="ts">
import EditableCatalogTable from '../components/EditableCatalogTable.vue';
import { wzlApi, type CreateWzlCatalog } from '../api/catalog';
import type { CatalogColumn } from '../types/catalogTable';

const columns: CatalogColumn[] = [
  { prop: 'model', label: '型号', type: 'text', width: 150 },
  { prop: 'function', label: '功能', type: 'text', width: 120 },
  { prop: 'stroke', label: '行程', type: 'text', width: 110 },
  { prop: 'innerHeight', label: '内高', width: 90 },
  { prop: 'innerWidth', label: '内宽', width: 90 },
  { prop: 'outerHeight', label: '外高', width: 90 },
  { prop: 'outerWidth', label: '外宽', width: 90 },
  { prop: 'minRadius', label: '最小R', width: 90 },
  { prop: 'recRadius', label: '推荐R', width: 90 },
  { prop: 'reservedK', label: '预留K', width: 90 },
  { prop: 'bendLength', label: '弯曲长', width: 100 },
  { prop: 'mountingH1', label: '安装H1', type: 'text', width: 110 },
  { prop: 'interferenceH2', label: '干涉H2', type: 'text', width: 110 },
  { prop: 'innerArea', label: '内空面积', width: 110, defaultValue: null },
  { prop: 'appPipes', label: '适用管线', type: 'text', width: 160 }
];
</script>

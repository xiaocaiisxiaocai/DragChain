<template>
  <EditableCatalogTable
    title="线槽型录"
    :columns="columns"
    :get-rows="trunkingApi.getAll"
    :create-row="row => trunkingApi.create(row as unknown as CreateTrunkingDto)"
    :update-row="(id, row) => trunkingApi.update(id, row as unknown as CreateTrunkingDto)"
    :delete-row="trunkingApi.delete"
  />
</template>

<script setup lang="ts">
import EditableCatalogTable from '../components/EditableCatalogTable.vue';
import { trunkingApi, type CreateTrunkingDto } from '../api/trunking';
import type { CatalogColumn } from '../types/catalogTable';

const columns: CatalogColumn[] = [
  { prop: 'model', label: '型号', type: 'text', minWidth: 220 },
  { prop: 'width', label: '宽', minWidth: 160 },
  { prop: 'height', label: '高', minWidth: 160 },
  {
    prop: 'crossSection',
    label: '面积',
    minWidth: 180,
    readonly: true,
    calculate: row => Number(row.width || 0) * Number(row.height || 0)
  }
];
</script>

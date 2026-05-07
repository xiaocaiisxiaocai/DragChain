<template>
  <div class="metric-item">
    <span>{{ label }}</span>
    <strong>{{ value ?? '-' }}<small v-if="unit">{{ unit }}</small></strong>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  label: string;
  value?: string | number;
  unit?: string;
}>();
</script>

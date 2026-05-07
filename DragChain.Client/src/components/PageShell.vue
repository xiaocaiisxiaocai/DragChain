<template>
  <div class="page-shell">
    <div v-if="$slots.toolbar" class="page-toolbar">
      <slot name="toolbar" />
    </div>
    <slot />
  </div>
</template>

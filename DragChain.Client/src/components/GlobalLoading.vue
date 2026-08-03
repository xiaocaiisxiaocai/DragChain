<template>
  <Transition name="fade">
    <div v-if="isLoading" class="global-loading">
      <div class="loading-spinner" />
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const isLoading = ref(false);
let loadingTimer: number | undefined;

router.beforeEach(() => {
  // 延迟显示 Loading，避免快速切换时闪烁
  loadingTimer = window.setTimeout(() => {
    isLoading.value = true;
  }, 200);
});

router.afterEach(() => {
  if (loadingTimer) {
    window.clearTimeout(loadingTimer);
    loadingTimer = undefined;
  }
  isLoading.value = false;
});
</script>

<style scoped>
.global-loading {
  position: fixed;
  top: 60px;
  left: 0;
  right: 0;
  height: 3px;
  background: transparent;
  z-index: 9999;
  pointer-events: none;
}

.loading-spinner {
  height: 100%;
  background: linear-gradient(90deg, #2563eb 0%, #3b82f6 50%, #2563eb 100%);
  background-size: 200% 100%;
  animation: loading-slide 1.5s ease-in-out infinite;
}

@keyframes loading-slide {
  0% {
    background-position: 200% 0;
    width: 0;
  }
  50% {
    width: 70%;
  }
  100% {
    background-position: -200% 0;
    width: 100%;
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

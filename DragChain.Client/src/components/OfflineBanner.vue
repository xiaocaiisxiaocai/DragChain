<template>
  <Transition name="slide-down">
    <div v-if="!isOnline" class="offline-banner">
      <div class="offline-content">
        <span class="offline-icon">⚠️</span>
        <span class="offline-text">网络连接已断开，部分功能可能无法使用</span>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { onBeforeUnmount } from 'vue';
import { useOnlineStatus } from '@/composables/useOnlineStatus';

const { isOnline, cleanup } = useOnlineStatus();

onBeforeUnmount(() => {
  cleanup();
});
</script>

<style scoped>
.offline-banner {
  position: fixed;
  top: 60px;
  left: 0;
  right: 0;
  z-index: 9998;
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  border-bottom: 2px solid #f59e0b;
  box-shadow: 0 2px 8px rgba(217, 119, 6, 0.15);
}

.offline-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 10px 20px;
  color: #78350f;
  font-size: 13px;
  font-weight: 600;
}

.offline-icon {
  font-size: 18px;
  animation: pulse 2s ease-in-out infinite;
}

.offline-text {
  letter-spacing: 0.3px;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.1);
  }
}

.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
</style>

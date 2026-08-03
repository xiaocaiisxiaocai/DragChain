import { ElMessage } from 'element-plus';
import type { ComponentPublicInstance } from 'vue';

/**
 * 全局错误处理器
 * @param error - 错误对象
 * @param instance - Vue 组件实例
 * @param info - 错误信息
 */
export function handleGlobalError(
  error: unknown,
  instance: ComponentPublicInstance | null,
  info: string
) {
  console.error('[Global Error]', error, info, instance);

  const message = getErrorMessage(error);

  // 避免重复显示相同的错误提示
  if (!lastErrorMessage || lastErrorMessage !== message || Date.now() - lastErrorTime > 3000) {
    ElMessage.error(message);
    lastErrorMessage = message;
    lastErrorTime = Date.now();
  }
}

let lastErrorMessage = '';
let lastErrorTime = 0;

/**
 * Promise 未捕获错误处理器
 */
export function handleUnhandledRejection(event: PromiseRejectionEvent) {
  console.error('[Unhandled Rejection]', event.reason);
  handleGlobalError(event.reason, null, 'Unhandled Promise Rejection');
}

/**
 * 包装异步函数，自动处理错误
 * @param fn - 异步函数
 * @param errorMessage - 自定义错误消息
 */
export function withErrorHandler<T extends (...args: any[]) => Promise<any>>(
  fn: T,
  errorMessage?: string
): T {
  return (async (...args: Parameters<T>) => {
    try {
      return await fn(...args);
    } catch (error) {
      const message = errorMessage || getErrorMessage(error, '操作失败');
      ElMessage.error(message);
      throw error;
    }
  }) as T;
}

export function getErrorMessage(error: unknown, fallback = '发生未知错误') {
  if (error instanceof Error) return error.message;
  if (typeof error === 'string') return error;
  return fallback;
}

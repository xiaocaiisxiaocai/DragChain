import type { LocalSlot } from '../stores/trunkingRuntimeState';

const DEFAULT_SLOT_NAME_PATTERN = /^槽位\d+$/;

/**
 * 槽位在数据中按编号从小到大保存，但实际安装方向是编号越大越靠上。
 * 所有依赖物理上下关系的展示与计算，都应使用这个顺序。
 */
export function getSlotsTopToBottom<T>(slots: readonly T[]) {
  return [...slots].reverse();
}

export function renumberDefaultSlots(slots: LocalSlot[]) {
  return slots.map((slot, index) => ({
    ...slot,
    name: DEFAULT_SLOT_NAME_PATTERN.test(slot.name.trim())
      ? `槽位${index + 1}`
      : slot.name
  }));
}

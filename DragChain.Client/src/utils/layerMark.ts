export type LayerMark = 'top' | 'bottom';

export const LAYER_MARK_OPTIONS: Array<{ label: string; value: LayerMark }> = [
  { label: '上', value: 'top' },
  { label: '下', value: 'bottom' }
];

export function normalizeLayerMark(value?: string | null): LayerMark {
  return value === 'bottom' ? 'bottom' : 'top';
}

export function getLayerMarkLabel(value?: string | null) {
  return LAYER_MARK_OPTIONS.find(option => option.value === normalizeLayerMark(value))?.label || '上';
}

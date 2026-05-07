import type { PipeType } from '../types';

export const PIPE_TYPE_LABELS: Record<string, string> = {
  tube: '气管',
  weak_cable: '弱电电缆',
  strong_cable: '强电电缆',
  cable: '弱电电缆',
  encoder: '编码器线',
  other: '其他'
};

export function getPipeDisplayType(pipe: Pick<PipeType, 'name' | 'type'>): string {
  return pipe.type === 'cable' ? 'weak_cable' : pipe.type;
}

export function getPipeDisplayLabel(pipe: Pick<PipeType, 'name' | 'type'>): string {
  return PIPE_TYPE_LABELS[getPipeDisplayType(pipe)] || pipe.type;
}

export function toBackendPipeType(type: string): string {
  return type;
}

import type { ActivePipe } from '../types';

export type SlotLayout = 'leftRight' | 'topBottom';
export type SlotSectionKey = 'left' | 'right' | 'top' | 'bottom';

export interface LocalSlotSection {
  key: 'top' | 'bottom';
  label: string;
  selectedTrunkingId: number | null;
  fillRatio: number | null;
  pipes: ActivePipe[];
}

export interface LocalSlot {
  id: string;
  name: string;
  layout: SlotLayout;
  leftTrunkingId: number | null;
  rightTrunkingId: number | null;
  leftFillRatio: number | null;
  rightFillRatio: number | null;
  pipes: ActivePipe[];
  sections: LocalSlotSection[];
}

export interface TrunkingRuntimeState {
  activeSlotLayout: SlotLayout;
  slots: LocalSlot[];
}

let state: TrunkingRuntimeState | null = null;

export function getTrunkingRuntimeState() {
  return cloneState(state);
}

export function setTrunkingRuntimeState(nextState: TrunkingRuntimeState) {
  state = cloneState(nextState);
}

function cloneState<T>(value: T): T {
  return value == null ? value : JSON.parse(JSON.stringify(value));
}

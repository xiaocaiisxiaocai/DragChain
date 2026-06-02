import type { ActivePipe } from '../types';
import type { LocalSlot, SlotSectionKey } from '../stores/trunkingRuntimeState';

export interface OrderedSegmentLayerRef {
  slotId: string;
  slotName: string;
  sectionKey: SlotSectionKey;
  sectionLabel: string;
  pipes: ActivePipe[];
}

export function getOrderedSegmentLayerRefs(slots: LocalSlot[], segmentIndex: number): OrderedSegmentLayerRef[] {
  if (!slots.length || segmentIndex < 0 || segmentIndex > slots.length) return [];

  if (segmentIndex === 0) {
    return [createLayerRef(slots[0], 'top')].filter(Boolean) as OrderedSegmentLayerRef[];
  }

  if (segmentIndex === slots.length) {
    return [createLayerRef(slots[slots.length - 1], 'bottom')].filter(Boolean) as OrderedSegmentLayerRef[];
  }

  return [
    createLayerRef(slots[segmentIndex - 1], 'bottom'),
    createLayerRef(slots[segmentIndex], 'top')
  ].filter(Boolean) as OrderedSegmentLayerRef[];
}

function createLayerRef(slot: LocalSlot | undefined, sectionKey: SlotSectionKey): OrderedSegmentLayerRef | null {
  if (!slot) return null;
  const section = slot.sections.find(item => item.key === sectionKey);
  if (!section) return null;

  return {
    slotId: slot.id,
    slotName: slot.name,
    sectionKey,
    sectionLabel: section.label,
    pipes: section.pipes
  };
}

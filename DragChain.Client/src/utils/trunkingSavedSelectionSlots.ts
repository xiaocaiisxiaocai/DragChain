import type {
  TrunkingCalcRequest,
  TrunkingSavedSelection,
  TrunkingSavedSourceSlot,
  TrunkingSlotRequest
} from '../types';
import { getSlotsTopToBottom } from './trunkingSlotOrdering';

export function getSavedSelectionSlotsTopToBottom(
  selection: TrunkingSavedSelection | null
): TrunkingSavedSourceSlot[] {
  if (!selection) return [];

  const slots = selection.sourceSlots?.length
    ? selection.sourceSlots
    : (selection.request.slots || []).map(createFallbackSourceSlot);

  return selection.request.slotOrder === 'bottomToTop'
    ? getSlotsTopToBottom(slots)
    : [...slots];
}

/** 将旧版保存请求统一为当前编辑器使用的 bottomToTop 内部顺序。 */
export function getRequestSlotsBottomToTop(request: TrunkingCalcRequest): TrunkingSlotRequest[] {
  const slots = request.slots || [];
  return request.slotOrder === 'bottomToTop'
    ? [...slots]
    : getSlotsTopToBottom(slots);
}

function createFallbackSourceSlot(slot: TrunkingSlotRequest): TrunkingSavedSourceSlot {
  return {
    id: slot.id,
    name: slot.name,
    sections: (slot.sections || []).map(section => ({
      key: section.key === 'bottom' ? 'bottom' : 'top',
      label: section.label,
      pipes: section.pipes.map(pipe => ({
        kind: 'pipe' as const,
        libId: pipe.pipeTypeId,
        qty: pipe.qty
      }))
    }))
  };
}

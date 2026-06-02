import { getOrderedSegmentLayerRefs } from './trunkingSegmentLayers';
import type { LocalSlot } from '../stores/trunkingRuntimeState';

function createSlot(id: string, name: string): LocalSlot {
  return {
    id,
    name,
    layout: 'ordered',
    leftTrunkingId: null,
    rightTrunkingId: null,
    leftFillRatio: null,
    rightFillRatio: null,
    pipes: [],
    sections: [
      { key: 'top', label: '上层', selectedTrunkingId: null, fillRatio: null, pipes: [{ kind: 'pipe', libId: 1, qty: 1 }] },
      { key: 'bottom', label: '下层', selectedTrunkingId: null, fillRatio: null, pipes: [{ kind: 'pipe', libId: 2, qty: 1 }] }
    ]
  };
}

const slots = [createSlot('slot-1', '槽位1'), createSlot('slot-2', '槽位2')];

const firstSegment = getOrderedSegmentLayerRefs(slots, 0);
if (firstSegment.length !== 1 || firstSegment[0].slotId !== 'slot-1' || firstSegment[0].sectionKey !== 'top') {
  throw new Error('第一条线槽段必须映射到第一个槽位的上层');
}

const middleSegment = getOrderedSegmentLayerRefs(slots, 1);
if (
  middleSegment.length !== 2 ||
  middleSegment[0].slotId !== 'slot-1' ||
  middleSegment[0].sectionKey !== 'bottom' ||
  middleSegment[1].slotId !== 'slot-2' ||
  middleSegment[1].sectionKey !== 'top'
) {
  throw new Error('中间线槽段必须映射到上槽位下层和下槽位上层');
}

const lastSegment = getOrderedSegmentLayerRefs(slots, 2);
if (lastSegment.length !== 1 || lastSegment[0].slotId !== 'slot-2' || lastSegment[0].sectionKey !== 'bottom') {
  throw new Error('最后一条线槽段必须映射到最后一个槽位的下层');
}

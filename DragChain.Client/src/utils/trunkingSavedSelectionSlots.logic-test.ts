import {
  getRequestSlotsBottomToTop,
  getSavedSelectionSlotsTopToBottom
} from './trunkingSavedSelectionSlots';
import type { TrunkingSavedSelection, TrunkingSavedSourceSlot } from '../types';

function createSourceSlot(id: string, name: string): TrunkingSavedSourceSlot {
  return {
    id,
    name,
    sections: [
      { key: 'top', label: '上层', pipes: [] },
      { key: 'bottom', label: '下层', pipes: [] }
    ]
  };
}

const currentSelection: TrunkingSavedSelection = {
  name: '当前顺序测试',
  request: {
    selectedTrunkingId: 0,
    pipes: [],
    slotOrder: 'bottomToTop',
    slots: []
  },
  result: null,
  sourceSlots: [
    createSourceSlot('slot-1', '槽位1'),
    createSourceSlot('slot-2', '槽位2')
  ]
};

const ordered = getSavedSelectionSlotsTopToBottom(currentSelection);
if (ordered.map((slot: TrunkingSavedSourceSlot) => slot.id).join(',') !== 'slot-2,slot-1') {
  throw new Error('当前 bottomToTop 保存选型必须反转为物理从上到下展示');
}
if (currentSelection.sourceSlots?.map(slot => slot.id).join(',') !== 'slot-1,slot-2') {
  throw new Error('生成保存选型展示顺序时不能修改原始保存数据');
}

const legacySelection: TrunkingSavedSelection = {
  name: '旧版顺序测试',
  request: {
    selectedTrunkingId: 0,
    pipes: [],
    slots: [
      { id: 'slot-top', name: '上槽位', layout: 'ordered', sections: [] },
      { id: 'slot-bottom', name: '下槽位', layout: 'ordered', sections: [] }
    ]
  },
  result: null,
  sourceSlots: [
    createSourceSlot('slot-top', '上槽位'),
    createSourceSlot('slot-bottom', '下槽位')
  ]
};

const legacyDisplay = getSavedSelectionSlotsTopToBottom(legacySelection);
if (legacyDisplay.map(slot => slot.id).join(',') !== 'slot-top,slot-bottom') {
  throw new Error('缺少 slotOrder 的旧保存数据必须按既有 topToBottom 顺序展示');
}

const legacyEditorSlots = getRequestSlotsBottomToTop(legacySelection.request);
if (legacyEditorSlots.map(slot => slot.id).join(',') !== 'slot-bottom,slot-top') {
  throw new Error('载入旧保存数据时必须转换为当前编辑器的 bottomToTop 内部顺序');
}
if (legacySelection.request.slots?.map(slot => slot.id).join(',') !== 'slot-top,slot-bottom') {
  throw new Error('旧保存请求的顺序转换不能修改原始数据');
}

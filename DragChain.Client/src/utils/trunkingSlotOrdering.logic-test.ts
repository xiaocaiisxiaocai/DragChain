import { getSlotsTopToBottom, renumberDefaultSlots } from './trunkingSlotOrdering';
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
      { key: 'top', label: '上层', selectedTrunkingId: null, fillRatio: null, pipes: [{ kind: 'pipe', libId: 1, qty: 2 }] },
      { key: 'bottom', label: '下层', selectedTrunkingId: null, fillRatio: null, pipes: [] }
    ]
  };
}

const slots = [
  createSlot('slot-a', '槽位1'),
  createSlot('slot-e', '槽位5')
];

const topToBottom = getSlotsTopToBottom(slots);
if (topToBottom.map(slot => slot.id).join(',') !== 'slot-e,slot-a') {
  throw new Error('槽位的物理顺序必须是编号较大的在上方');
}
if (slots.map(slot => slot.id).join(',') !== 'slot-a,slot-e') {
  throw new Error('生成物理顺序时不能修改原始槽位数组');
}

const renumbered = renumberDefaultSlots(slots);

if (renumbered.map(slot => slot.name).join(',') !== '槽位1,槽位2') {
  throw new Error('默认槽位名必须按当前顺序重新编号');
}

if (renumbered[1].id !== 'slot-e' || renumbered[1].sections[0].pipes[0].qty !== 2) {
  throw new Error('重排槽位名不能破坏槽位 id 或管线数据');
}

const customNamed = renumberDefaultSlots([
  createSlot('slot-a', '槽位1'),
  createSlot('slot-custom', '工位A')
]);

if (customNamed[1].name !== '工位A') {
  throw new Error('用户自定义槽位名不能被自动重排覆盖');
}

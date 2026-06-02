import { getLayerMarkLabel, normalizeLayerMark } from './layerMark';

if (normalizeLayerMark(undefined) !== 'top' || normalizeLayerMark('') !== 'top' || normalizeLayerMark('unknown') !== 'top') {
  throw new Error('上下标识默认和未知值必须归一为上');
}

if (normalizeLayerMark('top') !== 'top' || normalizeLayerMark('bottom') !== 'bottom') {
  throw new Error('上下标识必须保留上和下');
}

if (getLayerMarkLabel('') !== '上' || getLayerMarkLabel('top') !== '上' || getLayerMarkLabel('bottom') !== '下') {
  throw new Error('上下标识标签必须显示为上、下，空值默认显示为上');
}

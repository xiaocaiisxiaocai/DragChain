import type { CatalogColumn } from '../types/catalogTable';
import { applyCalculatedCatalogFields } from './catalogForm';

const columns: CatalogColumn[] = [
  { prop: 'width', label: '宽' },
  { prop: 'height', label: '高' },
  {
    prop: 'crossSection',
    label: '面积',
    readonly: true,
    calculate: row => Number(row.width || 0) * Number(row.height || 0)
  }
];

const form = { width: 25, height: 25, crossSection: 625 };

form.width = 40;
form.height = 30;
applyCalculatedCatalogFields(columns, form);

if (form.crossSection !== 1200) {
  throw new Error('编辑线槽宽和高后，面积必须自动按 宽×高 计算');
}

form.crossSection = 1;
applyCalculatedCatalogFields(columns, form);

if (form.crossSection !== 1200) {
  throw new Error('保存前必须重新计算面积，避免提交手动或旧面积');
}

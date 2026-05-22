import type { CatalogColumn } from '../types/catalogTable';

export function applyCalculatedCatalogFields(columns: CatalogColumn[], row: Record<string, unknown>) {
  columns.forEach(column => {
    if (column.calculate) {
      row[column.prop] = column.calculate(row);
    }
  });
}

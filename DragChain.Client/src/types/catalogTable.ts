export interface CatalogColumn {
  prop: string;
  label: string;
  type?: 'text' | 'number';
  width?: number;
  minWidth?: number;
  precision?: number;
  step?: number;
  defaultValue?: string | number | null;
}

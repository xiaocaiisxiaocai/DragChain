import { client } from './client';
import type { MeCatalog, WzlCatalog } from '../types';

export interface CreateWzlCatalog {
  model: string;
  function: string;
  stroke: string;
  innerHeight: number;
  innerWidth: number;
  outerHeight: number;
  outerWidth: number;
  minRadius: number;
  recRadius: number;
  reservedK: number;
  bendLength: number;
  mountingH1: string;
  interferenceH2: string;
  innerArea: number | null;
  appPipes: string;
}

export type UpdateWzlCatalog = Partial<CreateWzlCatalog>;

export interface CreateMeCatalog {
  baseModel: string;
  functionSelect: string;
  innerHeight: number;
  innerWidth: number;
  r1: number;
  r2: number;
  r3: number;
  r1Suffix: string;
  r2Suffix: string;
  r3Suffix: string;
  lp1: number;
  lp2: number;
  lp3: number;
  mountingH1: string;
  innerArea: number;
  maxWeight: number;
  spanBase: number;
  spanSlope: number;
}

export type UpdateMeCatalog = Partial<CreateMeCatalog>;

export const wzlApi = {
  getAll: () => client.get<WzlCatalog[]>('/Wzl'),
  create: (dto: CreateWzlCatalog) => client.post<WzlCatalog>('/Wzl', dto),
  update: (id: number, dto: UpdateWzlCatalog) => client.put<WzlCatalog>(`/Wzl/${id}`, dto),
  delete: (id: number) => client.del<void>(`/Wzl/${id}`),
  reset: () => client.post<{ message: string }>('/Wzl/reset')
};

export const meApi = {
  getAll: () => client.get<MeCatalog[]>('/Me'),
  create: (dto: CreateMeCatalog) => client.post<MeCatalog>('/Me', dto),
  update: (id: number, dto: UpdateMeCatalog) => client.put<MeCatalog>(`/Me/${id}`, dto),
  delete: (id: number) => client.del<void>(`/Me/${id}`),
  reset: () => client.post<{ message: string }>('/Me/reset')
};

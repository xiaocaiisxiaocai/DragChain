import { client } from './client';
import type { TrunkingCatalog, TrunkingCalcRequest, TrunkingCalcResponse } from '../types';

export interface CreateTrunkingDto {
  model: string;
  width: number;
  height: number;
  innerWidth: number;
  innerHeight: number;
  crossSection: number;
  material: string;
  remarks: string;
}

export interface UpdateTrunkingDto extends CreateTrunkingDto {}

export const trunkingApi = {
  getAll: () => client.get<TrunkingCatalog[]>('/trunking'),

  create: (dto: CreateTrunkingDto) =>
    client.post<TrunkingCatalog>('/trunking', dto),

  update: (id: number, dto: UpdateTrunkingDto) =>
    client.put<TrunkingCatalog>(`/trunking/${id}`, dto),

  delete: (id: number) =>
    client.del<void>(`/trunking/${id}`),

  calculate: (req: TrunkingCalcRequest) =>
    client.post<TrunkingCalcResponse>('/trunking/calc', req),

  reset: () =>
    client.post<{ message: string }>('/trunking/reset'),
};

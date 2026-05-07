import { client } from './client';
import type { TrunkingCalcRequest, TrunkingCalcResponse, TrunkingCatalog } from '../types';

export interface CreateTrunkingDto {
  model: string;
  width: number;
  height: number;
  crossSection: number;
}

export type UpdateTrunkingDto = CreateTrunkingDto;

export const trunkingApi = {
  getAll: () => client.get<TrunkingCatalog[]>('/trunking'),
  create: (dto: CreateTrunkingDto) => client.post<TrunkingCatalog>('/trunking', dto),
  update: (id: number, dto: UpdateTrunkingDto) => client.put<TrunkingCatalog>(`/trunking/${id}`, dto),
  delete: (id: number) => client.del<void>(`/trunking/${id}`),
  calculate: (req: TrunkingCalcRequest) => client.post<TrunkingCalcResponse>('/trunking/calc', req),
  reset: () => client.post<{ message: string }>('/trunking/reset')
};

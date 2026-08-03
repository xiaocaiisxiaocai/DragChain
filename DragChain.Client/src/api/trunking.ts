import { client } from './client';
import type { TrunkingCalcRequest, TrunkingCalcResponse, TrunkingCatalog, TrunkingSavedSelection, TrunkingSettings } from '../types';

export interface CreateTrunkingDto {
  model: string;
  width: number;
  height: number;
  crossSection: number;
  fillRatioLimit: number;
}

export type UpdateTrunkingDto = CreateTrunkingDto;

export const trunkingApi = {
  getAll: () => client.get<TrunkingCatalog[]>('/trunking'),
  create: (dto: CreateTrunkingDto) => client.post<TrunkingCatalog>('/trunking', dto),
  update: (id: number, dto: UpdateTrunkingDto) => client.put<TrunkingCatalog>(`/trunking/${id}`, dto),
  delete: (id: number) => client.del<void>(`/trunking/${id}`),
  getSettings: () => client.get<TrunkingSettings>('/trunking/settings'),
  updateSettings: (dto: TrunkingSettings) => client.put<TrunkingSettings>('/trunking/settings', dto),
  getSavedSelection: () => client.get<TrunkingSavedSelection | null>('/trunking/saved-selection'),
  getSavedSelections: () => client.get<TrunkingSavedSelection[]>('/trunking/saved-selections'),
  getSavedSelectionById: (id: string) => client.get<TrunkingSavedSelection>(`/trunking/saved-selections/${id}`),
  saveSelection: (dto: TrunkingSavedSelection) => client.put<TrunkingSavedSelection>('/trunking/saved-selection', dto),
  deleteSavedSelection: (id: string) => client.del<void>(`/trunking/saved-selections/${id}`),
  calculate: (req: TrunkingCalcRequest) => client.post<TrunkingCalcResponse>('/trunking/calc', req),
  reset: () => client.post<{ message: string }>('/trunking/reset')
};

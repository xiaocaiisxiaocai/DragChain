import { client } from './client';
import type { PipeModule } from '../types';

export interface CreatePipeModuleItem {
  pipeTypeId: number;
  qty: number;
}

export interface CreatePipeModule {
  name: string;
  description: string;
  items: CreatePipeModuleItem[];
}

export type UpdatePipeModule = CreatePipeModule;

export const pipeModulesApi = {
  getAll: () => client.get<PipeModule[]>('/PipeModules'),
  getById: (id: number) => client.get<PipeModule>(`/PipeModules/${id}`),
  create: (dto: CreatePipeModule) => client.post<PipeModule>('/PipeModules', dto),
  update: (id: number, dto: UpdatePipeModule) => client.put<void>(`/PipeModules/${id}`, dto),
  delete: (id: number) => client.del<void>(`/PipeModules/${id}`)
};

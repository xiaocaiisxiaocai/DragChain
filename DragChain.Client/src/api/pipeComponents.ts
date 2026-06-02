import { client } from './client';
import type { PipeComponent } from '../types';

export interface CreatePipeComponentItem {
  pipeTypeId: number;
  qty: number;
  layer: 'top' | 'bottom';
}

export interface CreatePipeComponent {
  name: string;
  description: string;
  items: CreatePipeComponentItem[];
}

export type UpdatePipeComponent = CreatePipeComponent;

export const pipeComponentsApi = {
  getAll: () => client.get<PipeComponent[]>('/PipeComponents'),
  getById: (id: number) => client.get<PipeComponent>(`/PipeComponents/${id}`),
  create: (dto: CreatePipeComponent) => client.post<PipeComponent>('/PipeComponents', dto),
  update: (id: number, dto: UpdatePipeComponent) => client.put<void>(`/PipeComponents/${id}`, dto),
  delete: (id: number) => client.del<void>(`/PipeComponents/${id}`)
};

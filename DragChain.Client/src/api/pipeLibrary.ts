import { client } from './client';
import type { PipeType } from '../types';

export interface CreatePipeType {
  name: string;
  type: string;
  diameter: number;
  weight: number;
  bendMultiplier: number;
}

export interface UpdatePipeType {
  name?: string;
  type?: string;
  diameter?: number;
  weight?: number;
  bendMultiplier?: number;
}

export const pipeLibraryApi = {
  getAll: () => client.get<PipeType[]>('/PipeLibrary'),
  create: (dto: CreatePipeType) => client.post<PipeType>('/PipeLibrary', dto),
  update: (id: number, dto: UpdatePipeType) => client.put<PipeType>(`/PipeLibrary/${id}`, dto),
  delete: (id: number) => client.del<void>(`/PipeLibrary/${id}`),
  reset: () => client.post<{ message: string }>('/PipeLibrary/reset'),
};

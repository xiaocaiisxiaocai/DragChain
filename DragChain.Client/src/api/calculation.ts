import { client } from './client';
import type { CalculationRequest, CalculationResponse } from '../types';

export const calculationApi = {
  calc: (req: CalculationRequest) =>
    client.post<CalculationResponse>('/Calculation/calc', req),
};

import { useState, useCallback, useEffect } from 'react';
import type { CalculationRequest, CalculationResponse, ActivePipe } from '../types';
import { calculationApi } from '../api/calculation';

export interface CalculationState {
  result: CalculationResponse | null;
  loading: boolean;
  error: string | null;
}

export function useCalculation() {
  const [state, setState] = useState<CalculationState>({
    result: null,
    loading: false,
    error: null,
  });

  const calculate = useCallback(async (req: CalculationRequest) => {
    setState(s => ({ ...s, loading: true, error: null }));
    try {
      const result = await calculationApi.calc(req);
      setState({ result, loading: false, error: null });
    } catch (e: unknown) {
      setState({ result: null, loading: false, error: e instanceof Error ? e.message : '計算失敗' });
    }
  }, []);

  return { ...state, calculate };
}

export interface ActivePipeState {
  pipes: ActivePipe[];
  setPipes: (pipes: ActivePipe[]) => void;
  addPipe: (libId: number) => void;
  removePipe: (index: number) => void;
  updateQty: (index: number, qty: number) => void;
}

export function useActivePipes(initial: ActivePipe[]) {
  const [pipes, setPipes] = useState<ActivePipe[]>(initial);

  const addPipe = useCallback((libId: number) => {
    setPipes(prev => {
      if (prev.find(p => p.libId === libId)) return prev;
      return [...prev, { libId, qty: 1 }];
    });
  }, []);

  const removePipe = useCallback((index: number) => {
    setPipes(prev => prev.filter((_, i) => i !== index));
  }, []);

  const updateQty = useCallback((index: number, qty: number) => {
    setPipes(prev => prev.map((p, i) => i === index ? { ...p, qty } : p));
  }, []);

  return { pipes, setPipes, addPipe, removePipe, updateQty };
}

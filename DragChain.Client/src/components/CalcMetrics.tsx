import React from 'react';
import type { CalculationResponse } from '../types';

interface Props {
  result: CalculationResponse | null;
}

export const CalcMetrics: React.FC<Props> = ({ result }) => {
  return (
    <div className="data-table-wrap">
      <div className="data-table-head"><div className="data-table-title">核算指標</div></div>
      <div style={{ padding: '12px 14px' }}>
        <div className="calc-grid">
          <div className="calc-item">
            <div className="calc-item-label">最小內高</div>
            <div><span className="calc-item-value">{result?.minHeight.toFixed(2) ?? '–'}</span><span className="calc-item-unit">mm</span></div>
          </div>
          <div className="calc-item">
            <div className="calc-item-label">最小彎曲R</div>
            <div><span className="calc-item-value">{result?.minRadius.toFixed(0) ?? '–'}</span><span className="calc-item-unit">mm</span></div>
          </div>
          <div className="calc-item">
            <div className="calc-item-label">管線面積</div>
            <div><span className="calc-item-value">{result?.totalArea.toFixed(1) ?? '–'}</span><span className="calc-item-unit">mm²</span></div>
          </div>
          <div className="calc-item">
            <div className="calc-item-label">最小內空</div>
            <div><span className="calc-item-value">{result?.minInnerArea.toFixed(1) ?? '–'}</span><span className="calc-item-unit">mm²</span></div>
          </div>
          <div className="calc-item">
            <div className="calc-item-label">總重量</div>
            <div><span className="calc-item-value">{result?.totalWeight.toFixed(4) ?? '–'}</span><span className="calc-item-unit">kg/m</span></div>
          </div>
          <div className="calc-item">
            <div className="calc-item-label">需架空長</div>
            <div><span className="calc-item-value">{(result?.needSpan ?? 0) > 0 ? result?.needSpan : '—'}</span><span className="calc-item-unit">mm</span></div>
          </div>
        </div>
      </div>
    </div>
  );
};

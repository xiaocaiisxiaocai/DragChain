import React from 'react';
import type { CalculationResponse } from '../types';

interface Props {
  result: CalculationResponse | null;
  loading: boolean;
  error: string | null;
}

export const ResultPanel: React.FC<Props> = ({ result, loading, error }) => {
  if (loading) {
    return (
      <div className="data-table-wrap">
        <div className="data-table-head"><div className="data-table-title">最終選定結論</div></div>
        <div style={{ padding: 20, textAlign: 'center', color: 'var(--text3)' }}>
          計算中...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="data-table-wrap">
        <div className="data-table-head"><div className="data-table-title">最終選定結論</div></div>
        <div style={{ padding: 20, textAlign: 'center', color: 'var(--red)' }}>
          {error}
        </div>
      </div>
    );
  }

  if (!result) {
    return (
      <div className="data-table-wrap">
        <div className="data-table-head"><div className="data-table-title">最終選定結論</div></div>
        <div style={{ padding: 20, textAlign: 'center', color: 'var(--text3)' }}>
          請填寫管線清單
        </div>
      </div>
    );
  }

  const iconMap: Record<string, string> = { ok: '✓', warn: '↕', err: '⚠' };
  const boxClass = `result-box ${result.resultStatus}`;

  return (
    <div className="data-table-wrap">
      <div className="data-table-head"><div className="data-table-title">最終選定結論</div></div>
      <div style={{ padding: 14 }}>
        <div className={boxClass}>
          <div className="result-icon" id="resultIcon">{iconMap[result.resultStatus] || '⟳'}</div>
          <div style={{ flex: 1 }}>
            <div className="result-label">最終選定型號 + 長度</div>
            <div className="result-model">{result.resultMessage}</div>
            <div className="result-note">
              {result.finalModel
                ? `彎曲長度 Lp=${result.finalModel.lp}mm · 拖鏈長度 Lk=${result.finalModel.lk}mm`
                : result.preliminaryModel
                ? `彎曲長度 Lp=${result.preliminaryModel.lp}mm · 拖鏈長度 Lk=${result.preliminaryModel.lk}mm`
                : '—'}
            </div>
            {result.strategyNote && (
              <div className="result-strategy" dangerouslySetInnerHTML={{ __html: `<strong>📌</strong> ${result.strategyNote}` }} />
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

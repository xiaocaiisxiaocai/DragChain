import React from 'react';
import type { PipeType, ActivePipe } from '../types';

interface Props {
  pipeLib: PipeType[];
  activePipes: ActivePipe[];
  onQtyChange: (index: number, qty: number) => void;
  onDelete: (index: number) => void;
  onAddFromLib: () => void;
  maxBend: number;
  maxBendDia: number;
}

const TYPE_MAP = {
  tube:    { label: '氣管',    cls: 'pipe-badge-tube' },
  cable:   { label: '電纜',    cls: 'pipe-badge-cable' },
  encoder: { label: '編碼器',  cls: 'pipe-badge-encoder' },
  other:   { label: '其他',    cls: 'pipe-badge-other' },
};

export const PipeTable: React.FC<Props> = ({ pipeLib, activePipes, onQtyChange, onDelete, onAddFromLib, maxBend, maxBendDia }) => {
  const pipeMap = pipeLib.reduce((acc, p) => { acc[p.id] = p; return acc; }, {} as Record<number, PipeType>);

  const pipeArea = (p: PipeType, qty: number) =>
    qty > 0 ? (Math.PI * Math.pow(p.diameter / 2, 2) * qty).toFixed(1) : '–';

  const pipeWeight = (p: PipeType, qty: number) =>
    qty > 0 ? (qty * p.weight).toFixed(3) : '–';

  const pipeBend = (p: PipeType, qty: number) =>
    qty > 0 ? p.diameter * p.bendMultiplier : 0;

  return (
    <div className="section-card">
      <div className="section-head" onClick={e => (e.currentTarget.parentElement as HTMLElement).classList.toggle('collapsed')}>
        <div className="section-num">2</div>
        <div className="section-title">管線清單（填寫數量）</div>
        <span className="section-head-action" onClick={e => { e.stopPropagation(); onAddFromLib(); }}>管線庫</span>
        <span className="section-chevron" style={{ marginLeft: 6 }}>▾</span>
      </div>
      <div className="section-body" style={{ padding: '4px 0px' }}>
        <div style={{ overflowX: 'auto', maxHeight: 280, overflowY: 'auto' }}>
          <table className="pipe-table" style={{ minWidth: 600 }}>
            <thead>
              <tr>
                <th style={{ textAlign: 'left', minWidth: 110, paddingLeft: 8 }}>管線名稱</th>
                <th style={{ minWidth: 48 }}>數量</th>
                <th style={{ minWidth: 40 }}>直徑<br />mm</th>
                <th style={{ minWidth: 52 }}>單根重<br />kg/m</th>
                <th style={{ minWidth: 62 }}>分類</th>
                <th style={{ minWidth: 56 }}>重量核算<br />kg/m</th>
                <th style={{ minWidth: 52 }}>彎曲R<br />mm</th>
                <th style={{ minWidth: 52 }}>面積<br />mm²</th>
                <th style={{ minWidth: 28 }}></th>
              </tr>
            </thead>
            <tbody>
              {activePipes.map((ap, i) => {
                const p = pipeMap[ap.libId];
                if (!p) return null;
                const qty = ap.qty || 0;
                const bend = pipeBend(p, qty);
                const isMax = bend > 0 && bend === maxBend;
                const tInfo = TYPE_MAP[p.type as keyof typeof TYPE_MAP] || TYPE_MAP.other;
                return (
                  <tr key={i}>
                    <td style={{ padding: '4px 4px 4px 12px' }}>
                      <span style={{ fontSize: 11, color: 'var(--text2)' }}>{p.name}</span>
                    </td>
                    <td>
                      <input type="number" className="pipe-num-edit" value={qty} min={0} step={1}
                        onChange={e => onQtyChange(i, parseInt(e.target.value) || 0)} />
                    </td>
                    <td className="pipe-fixed">{p.diameter}</td>
                    <td className="pipe-derived">{p.weight}</td>
                    <td style={{ textAlign: 'center' }}>
                      <span className={`pipe-type-badge ${tInfo.cls}`}>{tInfo.label}</span>
                    </td>
                    <td className="pipe-derived">{pipeWeight(p, qty)}</td>
                    <td className="pipe-derived" style={{
                      color: isMax ? 'var(--red)' : 'rgba(245,158,11,0.7)',
                      fontWeight: isMax ? 700 : 'normal',
                    }}>
                      {qty > 0 ? bend : '–'}
                    </td>
                    <td className="pipe-derived">{pipeArea(p, qty)}</td>
                    <td>
                      <button className="pipe-del-btn" onClick={() => onDelete(i)}>×</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div style={{ padding: '0 8px' }}>
          <button className="add-pipe-btn" onClick={onAddFromLib}>＋ 從管線庫新增</button>
        </div>
      </div>
    </div>
  );
};

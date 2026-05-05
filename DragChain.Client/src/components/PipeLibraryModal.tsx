import React, { useEffect } from 'react';
import type { PipeType } from '../types';

interface Props {
  open: boolean;
  onClose: () => void;
  pipeLib: PipeType[];
  onLoad: () => void;
}

export const PipeLibraryModal: React.FC<Props> = ({ open, onClose, pipeLib, onLoad }) => {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    if (open) document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box">
        <div className="modal-head">
          <div className="modal-title">管線庫維護</div>
          <button className="modal-close" onClick={onClose}>×</button>
        </div>
        <div className="modal-body">
          <p className="info-hint" style={{ marginBottom: 'var(--space-4)' }}>
            管線庫是所有類型管線的預設資料庫。選型計算時可從庫中選取加入清單。
          </p>
          <table className="lib-tbl">
            <thead>
              <tr>
                <th style={{ textAlign: 'left' }}>名稱</th>
                <th>類型</th>
                <th>外徑 mm</th>
                <th>重量 kg/m</th>
                <th>彎曲係數</th>
              </tr>
            </thead>
            <tbody>
              {pipeLib.map(p => (
                <tr key={p.id}>
                  <td style={{ fontSize: 12, color: 'var(--text2)' }}>{p.name}</td>
                  <td style={{ textAlign: 'center', fontSize: 11 }}>
                    <span className={`pipe-type-badge ${
                      p.type === 'tube' ? 'pipe-badge-tube' :
                      p.type === 'encoder' ? 'pipe-badge-encoder' :
                      p.type === 'cable' ? 'pipe-badge-cable' : 'pipe-badge-other'
                    }`}>
                      {p.type === 'tube' ? '氣管/水管' : p.type === 'encoder' ? '編碼器線' : p.type === 'cable' ? '電纜' : '其他'}
                    </span>
                  </td>
                  <td style={{ textAlign: 'center', fontFamily: 'var(--mono)', fontSize: 12 }}>{p.diameter}</td>
                  <td style={{ textAlign: 'center', fontFamily: 'var(--mono)', fontSize: 12 }}>{p.weight}</td>
                  <td style={{ textAlign: 'center', fontSize: 11, color: 'var(--text3)' }}>×{p.bendMultiplier}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="modal-footer">
          <button className="btn btn-primary" onClick={onClose}>完成</button>
          <span style={{ fontSize: 11, color: 'var(--text3)', marginLeft: 'auto' }}>共 {pipeLib.length} 條記錄</span>
        </div>
      </div>
    </div>
  );
};

import React, { useEffect } from 'react';
import type { PipeType } from '../types';

interface Props {
  open: boolean;
  onClose: () => void;
  pipeLib: PipeType[];
  activePipes: { libId: number }[];
  onConfirm: (ids: number[]) => void;
}

const TYPE_LABELS: Record<string, string> = {
  tube: '氣管 / 水管', cable: '電纜', encoder: '編碼器線', other: '其他',
};

export const AddFromLibModal: React.FC<Props> = ({ open, onClose, pipeLib, activePipes, onConfirm }) => {
  const [selected, setSelected] = React.useState<Set<number>>(new Set());

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    if (open) document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  if (!open) return null;

  const groups = ['tube', 'cable', 'encoder', 'other'];
  const activeIds = new Set(activePipes.map(p => p.libId));

  const toggle = (id: number) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const handleConfirm = () => {
    onConfirm(Array.from(selected));
    setSelected(new Set());
    onClose();
  };

  return (
    <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box" style={{ width: 500 }}>
        <div className="modal-head">
          <div className="modal-title">從管線庫新增</div>
          <button className="modal-close" onClick={onClose}>×</button>
        </div>
        <div className="modal-body">
          <p className="info-hint" style={{ marginBottom: 'var(--space-4)' }}>
            選擇要加入當前清單的管線（勾選後點「加入」）
          </p>
          {groups.map(type => {
            const items = pipeLib.filter(p => p.type === type);
            if (!items.length) return null;
            return (
              <div key={type} style={{ marginBottom: 'var(--space-4)' }}>
                <div className="lib-group-label">
                  {TYPE_LABELS[type] || type.toUpperCase()}
                </div>
                {items.map(p => (
                  <label key={p.id} className={`lib-item ${selected.has(p.id) ? 'selected' : ''} ${activeIds.has(p.id) ? 'disabled' : ''}`}>
                    <input type="checkbox"
                      checked={selected.has(p.id) || activeIds.has(p.id)}
                      disabled={activeIds.has(p.id)}
                      onChange={() => toggle(p.id)} />
                    <span className="lib-item-name">{p.name}</span>
                    <span className="lib-item-meta">Φ{p.diameter} · {p.weight}kg/m</span>
                  </label>
                ))}
              </div>
            );
          })}
        </div>
        <div className="modal-footer">
          <button className="btn btn-primary" onClick={handleConfirm}>加入選中</button>
          <button className="btn btn-ghost" onClick={onClose}>取消</button>
        </div>
      </div>
    </div>
  );
};

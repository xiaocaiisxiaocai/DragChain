import React, { useEffect } from 'react';

interface Props {
  isOpen: boolean;
  title?: string;
  message: string;
  confirmLabel?: string;
  confirmDanger?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const ConfirmModal: React.FC<Props> = ({
  isOpen,
  title = '確認操作',
  message,
  confirmLabel = '確認',
  confirmDanger = false,
  onConfirm,
  onCancel,
}) => {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (!isOpen) return;
      if (e.key === 'Enter') { e.preventDefault(); onConfirm(); }
      if (e.key === 'Escape') onCancel();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [isOpen, onConfirm, onCancel]);

  if (!isOpen) return null;

  return (
    <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) onCancel(); }}>
      <div className="modal-box" style={{ width: 420 }}>
        <div className="modal-head">
          <div className="modal-title">{title}</div>
          <button className="modal-close" onClick={onCancel}>×</button>
        </div>
        <div className="modal-body">
          <p style={{ fontSize: 13, color: 'var(--text2)', lineHeight: 1.6 }}>{message}</p>
        </div>
        <div className="modal-footer">
          <button className={confirmDanger ? 'btn btn-danger' : 'btn btn-primary'} onClick={onConfirm}>
            {confirmLabel}
          </button>
          <button className="btn btn-ghost" onClick={onCancel}>取消</button>
        </div>
      </div>
    </div>
  );
};

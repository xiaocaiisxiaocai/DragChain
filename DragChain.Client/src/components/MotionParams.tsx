import React from 'react';

interface Props {
  motionType: string;
  stroke: number;
  lmOffset: number;
  onMotionChange: (v: string) => void;
  onStrokeChange: (v: number) => void;
  onLmChange: (v: number) => void;
  needSpan: number;
}

export const MotionParams: React.FC<Props> = ({ motionType, stroke, lmOffset, onMotionChange, onStrokeChange, onLmChange, needSpan }) => {
  return (
    <div className="section-card">
      <div className="section-head" onClick={e => (e.currentTarget.parentElement as HTMLElement).classList.toggle('collapsed')}>
        <div className="section-num">3</div>
        <div className="section-title">運動參數</div>
        <span className="section-chevron">▾</span>
      </div>
      <div className="section-body">
        <div className="field-row">
          <div className="field-label">運動方式</div>
          <select className="field-input" value={motionType} onChange={e => onMotionChange(e.target.value)}>
            <option value="横移">横移</option>
            <option value="升降">升降</option>
          </select>
        </div>
        <div className="field-row">
          <div className="field-label">移動行程 (mm)</div>
          <input type="number" className="field-input" value={stroke} min={1} step={1}
            onChange={e => onStrokeChange(parseInt(e.target.value) || 0)} />
        </div>
        <div className="field-row">
          <div className="field-label">固定端偏移 Lm (mm)</div>
          <input type="number" className="field-input" value={lmOffset} min={0} step={1}
            onChange={e => onLmChange(parseInt(e.target.value) || 0)} />
        </div>
        <div style={{ fontSize: '10.5px', color: 'var(--text3)', padding: '4px 0' }}>
          {motionType === '横移'
            ? `需要架空長度 = ${stroke} ÷ 2 = ${Math.round(stroke / 2)} mm`
            : '升降模式：管線做垂直運動，無需架空判定'}
        </div>
      </div>
    </div>
  );
};

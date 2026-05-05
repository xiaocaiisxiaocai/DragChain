import React from 'react';

interface Props {
  sensorCount: number;
  magnetCount: number;
  coreCount: number;
  onSensorChange: (v: number) => void;
  onMagnetChange: (v: number) => void;
}

export const SensorConfig: React.FC<Props> = ({ sensorCount, magnetCount, coreCount, onSensorChange, onMagnetChange }) => {
  return (
    <div className="section-card">
      <div className="section-head" onClick={e => (e.currentTarget.parentElement as HTMLElement).classList.toggle('collapsed')}>
        <div className="section-num">1</div>
        <div className="section-title">感應器 / 信號線芯數</div>
        <span className="section-chevron">▾</span>
      </div>
      <div className="section-body">
        <div className="field-row">
          <div className="field-label">感應器個數</div>
          <input type="number" className="field-input" value={sensorCount} min={0} step={1}
            onChange={e => onSensorChange(parseInt(e.target.value) || 0)} />
        </div>
        <div className="field-row">
          <div className="field-label">非同動氣缸磁環組數</div>
          <input type="number" className="field-input" value={magnetCount} min={0} step={1}
            onChange={e => onMagnetChange(parseInt(e.target.value) || 0)} />
        </div>
        <div className="divider" />
        <div className="field-row">
          <div className="field-label" style={{ color: 'var(--text3)', fontSize: '11.5px' }}>需要電纜芯數（自動計算）</div>
          <input type="text" className="field-input computed" value={coreCount} readOnly />
        </div>
      </div>
    </div>
  );
};

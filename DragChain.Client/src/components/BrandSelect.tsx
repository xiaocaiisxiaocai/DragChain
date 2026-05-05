import React from 'react';

interface Props {
  brand: string;
  onChange: (brand: string) => void;
}

export const BrandSelect: React.FC<Props> = ({ brand, onChange }) => {
  return (
    <div className="section-card">
      <div className="section-head" onClick={e => (e.currentTarget.parentElement as HTMLElement).classList.toggle('collapsed')}>
        <div className="section-num">0</div>
        <div className="section-title">選擇拖鏈品牌</div>
        <span className="section-chevron">▾</span>
      </div>
      <div className="section-body">
        <div className="field-row">
          <div className="field-label">品牌 / 系列</div>
          <select className="field-input" value={brand} onChange={e => onChange(e.target.value)}>
            <option value="wzl">沃德無塵拖鏈 WZL</option>
            <option value="me">犸幕普通拖鏈 ME</option>
          </select>
        </div>
      </div>
    </div>
  );
};

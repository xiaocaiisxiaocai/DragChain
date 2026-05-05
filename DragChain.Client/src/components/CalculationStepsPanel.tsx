import React from 'react';
import type { CalculationSteps } from '../types';

interface Props {
  steps: CalculationSteps;
  tubeBend: number;
  cableBend: number;
  encoderBend: number;
  maxBend: number;
}

export const CalculationStepsPanel: React.FC<Props> = ({ steps, tubeBend, cableBend, encoderBend, maxBend }) => {
  const rows = [];
  if (tubeBend > 0)    rows.push({ label: `氣管彎曲半徑`, val: tubeBend, isMax: tubeBend === maxBend });
  if (cableBend > 0)   rows.push({ label: `電纜彎曲半徑`, val: cableBend, isMax: cableBend === maxBend && tubeBend !== maxBend });
  if (encoderBend > 0) rows.push({ label: `編碼器線彎曲半徑`, val: encoderBend, isMax: encoderBend === maxBend });

  return (
    <div className="data-table-wrap">
      <div className="data-table-head">
        <div className="data-table-title">計算過程明細</div>
        <span className="badge badge-blue">對應 Excel 步驟 3-1 ~ 結論</span>
      </div>
      <div className="step-panel">
        {/* 3-1 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-1</span>核算拖鏈最小內高 / mm</div>
          <div className="step-row">
            <span className="step-row-label">最大管線外徑 × 1.25</span>
            <span className="step-row-val">{steps.step3_1_MinHeight}</span>
          </div>
        </div>
        {/* 3-2 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-2</span>核算拖鏈推薦彎曲半徑</div>
          <div className="step-row">
            <span className="step-row-label">氣管彎曲半徑核對</span>
            <span className="step-row-val">{steps.step3_2_BendTube}</span>
            <span className="step-row-note">普通氣管允許最小彎曲半徑，通常 ≥ 8 × 外徑</span>
          </div>
          <div className="step-row">
            <span className="step-row-label">電纜彎曲半徑核對</span>
            <span className="step-row-val">{steps.step3_2_BendCable}</span>
            <span className="step-row-note">耐曲折電纜允許最小彎曲半徑，通常 ≥ 8 × 外徑</span>
          </div>
          <div className="step-row step-row-highlight">
            <span className="step-row-label">拖鏈最小彎曲半徑 / mm</span>
            <span className="step-row-val">{steps.step3_2_BendMax}</span>
            <span className="step-row-note">伺服編碼器線允許最小彎曲半徑，通常 ≥ 13 × 外徑</span>
          </div>
          {rows.length > 0 && (
            <div style={{ padding: '6px 0' }}>
              {rows.map((r, i) => (
                <div key={i} className={`bend-row ${r.isMax ? 'bend-row-max' : ''}`}>
                  <span className="bend-row-label">{r.label}</span>
                  <span className="bend-row-val">{r.val} mm{r.isMax ? ' ◀ 控制值' : ''}</span>
                </div>
              ))}
            </div>
          )}
        </div>
        {/* 3-3 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-3</span>核算旋轉拖鏈內空</div>
          <div className="step-row">
            <span className="step-row-label">管線面積總和</span>
            <span className="step-row-val">{steps.step3_3_AreaSum}</span>
          </div>
          <div className="step-row">
            <span className="step-row-label">管線與拖鏈內部面積佔比</span>
            <span className="step-row-val">{steps.step3_3_Ratio}</span>
            <span className="step-row-note">{steps.step3_3_Ratio?.includes('60%') ? '無塵拖鏈建議內空佔比 60%' : '普通拖鏈直角可用面積減少，建議取 55%'}</span>
          </div>
          <div className="step-row step-row-highlight">
            <span className="step-row-label">拖鏈最小內部面積 / mm²</span>
            <span className="step-row-val">{steps.step3_3_MinArea}</span>
          </div>
        </div>
        {/* 3-4 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-4</span>初步選定拖鏈型號</div>
          <div className="step-row step-row-highlight">
            <span className="step-row-label">初步選定型號</span>
            <span className="step-row-val">{steps.step3_4_PrelimModel}</span>
          </div>
        </div>
        {/* 3-5 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-5</span>核算架空長度</div>
          <div className="step-row"><span className="step-row-label">運動方式</span><span className="step-row-val">{steps.step3_5_Motion}</span></div>
          <div className="step-row"><span className="step-row-label">移動行程</span><span className="step-row-val">{steps.step3_5_Stroke}</span></div>
          <div className="step-row"><span className="step-row-label">拖鏈固定端偏移中心點距離 Lm</span><span className="step-row-val">{steps.step3_5_Lm}</span></div>
          <div className="step-row"><span className="step-row-label">初步選定彎曲長度 Lp</span><span className="step-row-val">{steps.step3_5_PrelimLp}</span></div>
          <div className="step-row">
            <span className="step-row-label">初步選定拖鏈長度 Lk</span>
            <span className="step-row-val">{steps.step3_5_PrelimLk}</span>
            <span className="step-row-note">ROUNDUP(行程÷2 + Lm + Lp, -1)</span>
          </div>
          <div className="step-row step-row-highlight">
            <span className="step-row-label">初步選定型號 + 長度</span>
            <span className="step-row-val">{steps.step3_5_PrelimFull}</span>
          </div>
        </div>
        {/* 3-6 */}
        <div className="step-block">
          <div className="step-block-title"><span className="step-tag">3-6</span>架空判定與最終選定</div>
          <div className="step-row">
            <span className="step-row-label">需要架空長度</span>
            <span className="step-row-val">{steps.step3_6_NeedSpan}</span>
            <span className="step-row-note">横移 = 行程÷2 ；升降無需計算</span>
          </div>
          <div className="step-row"><span className="step-row-label">負載重量 kg/m</span><span className="step-row-val">{steps.step3_6_Load}</span></div>
          <div className="step-row"><span className="step-row-label">判定初選拖鏈架空是否滿足</span><span className="step-row-val">{steps.step3_6_SpanOk}</span></div>
          <div className="step-row"><span className="step-row-label">選定滿足架空的拖鏈</span><span className="step-row-val">{steps.step3_6_FinalModel}</span></div>
          <div className="step-row"><span className="step-row-label">彎曲長度 Lp（根據右表選擇）</span><span className="step-row-val">{steps.step3_6_FinalLp}</span></div>
          <div className="step-row step-row-highlight">
            <span className="step-row-label">選定拖鏈長度 Lk</span>
            <span className="step-row-val">{steps.step3_6_FinalLk}</span>
          </div>
        </div>
        {/* 4 */}
        <div className="step-block step-block-strategy">
          <div className="step-block-title"><span className="step-tag step-tag-warn">4</span>架空長度超出對策</div>
          <div className="step-strategy-row"><b>4-1：</b>若長度超出，可增加龍骨系統，單內空會減少，需與廠商確認管線排布；或加大拖鏈規格</div>
          <div className="step-strategy-row"><b>4-2：</b>若長度超出，可增加輔助輪支撐，但會增加拖鏈磨損</div>
        </div>
      </div>
    </div>
  );
};

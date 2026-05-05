import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { PipeType, ActivePipe, TrunkingCalcResponse, TrunkingCatalog } from '../types';
import { trunkingApi } from '../api/trunking';
import { AddFromLibModal } from './AddFromLibModal';
import { PipeLibraryModal } from './PipeLibraryModal';

interface Props {
  pipeLib: PipeType[];
  activePipes: ActivePipe[];
  onPipesChange: (pipes: ActivePipe[]) => void;
}

const MIN_LEFT_PX = 280;
const MAX_LEFT_PX = 700;

const TYPE_MAP: Record<string, { label: string; cls: string }> = {
  tube:    { label: '氣管',    cls: 'pipe-badge-tube' },
  cable:   { label: '電纜',    cls: 'pipe-badge-cable' },
  encoder: { label: '編碼器',  cls: 'pipe-badge-encoder' },
  other:   { label: '其他',    cls: 'pipe-badge-other' },
};

const ok = (b: boolean) => b
  ? <span className="status-ok">OK</span>
  : <span className="status-ng">NG</span>;

export const TrunkingCalcPage: React.FC<Props> = ({ pipeLib, activePipes, onPipesChange }) => {
  const [trunkingList, setTrunkingList] = useState<TrunkingCatalog[]>([]);
  const [selectedTrunkingId, setSelectedTrunkingId] = useState<number>(0);
  const [fillRatio, setFillRatio] = useState<number>(75);
  const [result, setResult] = useState<TrunkingCalcResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showLibModal, setShowLibModal] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);

  const [leftWidth, setLeftWidth] = useState(420);
  const isDragging = useRef(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const loadTrunkingList = useCallback(() => {
    trunkingApi.getAll().then(setTrunkingList).catch(console.error);
  }, []);

  useEffect(() => { loadTrunkingList(); }, [loadTrunkingList]);

  const triggerCalc = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const req = {
        selectedTrunkingId: selectedTrunkingId,
        fillRatio: fillRatio / 100,
        pipes: activePipes.map(p => ({ pipeTypeId: p.libId, qty: p.qty }))
      };
      const data = await trunkingApi.calculate(req);
      setResult(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : '計算失敗');
    } finally {
      setLoading(false);
    }
  }, [selectedTrunkingId, fillRatio, activePipes]);

  useEffect(() => {
    const timer = setTimeout(triggerCalc, 300);
    return () => clearTimeout(timer);
  }, [triggerCalc]);

  const pipeMap = pipeLib.reduce((acc, p) => { acc[p.id] = p; return acc; }, {} as Record<number, PipeType>);

  const handleQtyChange = (index: number, qty: number) => {
    onPipesChange(activePipes.map((p, i) => i === index ? { ...p, qty } : p));
  };

  const handleDelete = (index: number) => {
    onPipesChange(activePipes.filter((_, i) => i !== index));
  };

  const handleAddFromLib = (ids: number[]) => {
    const existing = new Set(activePipes.map(p => p.libId));
    const added = ids.filter(id => !existing.has(id)).map(id => ({ libId: id, qty: 1 }));
    onPipesChange([...activePipes, ...added]);
    setShowAddModal(false);
  };

  const onDividerMouseDown = useCallback((e: React.MouseEvent) => {
    if (window.innerWidth <= 768) return;
    e.preventDefault();
    isDragging.current = true;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  useEffect(() => {
    const onMouseMove = (e: MouseEvent) => {
      if (isDragging.current && containerRef.current) {
        const rect = containerRef.current.getBoundingClientRect();
        setLeftWidth(Math.min(MAX_LEFT_PX, Math.max(MIN_LEFT_PX, e.clientX - rect.left)));
      }
    };
    const onMouseUp = () => {
      isDragging.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
    return () => {
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseup', onMouseUp);
    };
  }, []);

  const steps = result?.steps;
  const iconMap: Record<string, string> = { ok: '✓', warn: '↕', err: '⚠' };

  return (
    <div className="trunking-page" ref={containerRef}>
      {/* 左側：線槽選擇 + 管線管理 */}
      <div className="trunking-left" style={{ width: leftWidth, minWidth: leftWidth }}>
      
        {/* 選擇線槽 */}
        <div className="section-card">
          <div className="section-head">
            <div className="section-title">選擇線槽型號</div>
          </div>
          <div className="section-body">
            <select
              className="field-input"
              value={selectedTrunkingId}
              onChange={e => setSelectedTrunkingId(Number(e.target.value))}
              style={{ width: '100%', marginBottom: 8 }}
            >
              <option value={0}>— 請選擇線槽 —</option>
              {trunkingList.map(t => (
                <option key={t.id} value={t.id}>
                  {t.model}（{t.innerWidth}×{t.innerHeight}mm）
                </option>
              ))}
            </select>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
              <span style={{ fontSize: 11, color: 'var(--text2)', whiteSpace: 'nowrap' }}>填充率上限</span>
              <input
                type="number"
                className="field-input"
                value={fillRatio}
                onChange={e => setFillRatio(Math.min(100, Math.max(1, parseInt(e.target.value) || 75)))}
                min={1}
                max={100}
                style={{ width: 60, textAlign: 'center' }}
              />
              <span style={{ fontSize: 11, color: 'var(--text2)' }}>%</span>
            </div>
            {selectedTrunkingId > 0 && (
              <div style={{ fontSize: 11, color: 'var(--text3)', marginTop: 6, display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                <span>外：{trunkingList.find(t => t.id === selectedTrunkingId)?.width}×{trunkingList.find(t => t.id === selectedTrunkingId)?.height}mm</span>
                <span>截面積：{trunkingList.find(t => t.id === selectedTrunkingId)?.crossSection}mm²</span>
              </div>
            )}
          </div>
        </div>

        {/* 管線清單 */}
        <div className="section-card" style={{ flex: 1 }}>
          <div className="section-head">
            <div className="section-title">管線清單</div>
            <span className="section-head-action" onClick={() => setShowLibModal(true)}>管線庫</span>
          </div>
          <div className="section-body" style={{ padding: '4px 0' }}>
            <div style={{ maxHeight: 280, overflowY: 'auto' }}>
              <table className="pipe-table" style={{ minWidth: 320 }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', minWidth: 100, paddingLeft: 8 }}>管線</th>
                    <th style={{ minWidth: 40 }}>數量</th>
                    <th style={{ minWidth: 40 }}>直徑</th>
                    <th style={{ minWidth: 48 }}>面積</th>
                    <th style={{ minWidth: 24 }}></th>
                  </tr>
                </thead>
                <tbody>
                  {activePipes.length === 0 && (
                    <tr>
                      <td colSpan={5} style={{ textAlign: 'center', color: 'var(--text3)', padding: 12 }}>尚無管線</td>
                    </tr>
                  )}
                  {activePipes.map((ap, i) => {
                    const p = pipeMap[ap.libId];
                    if (!p) return null;
                    const qty = ap.qty || 0;
                    const area = qty > 0 ? (Math.PI * Math.pow(p.diameter / 2, 2) * qty).toFixed(0) : '–';
                    return (
                      <tr key={i}>
                        <td style={{ padding: '3px 4px 3px 8px', fontSize: 11 }}>{p.name}</td>
                        <td>
                          <input type="number" className="pipe-num-edit" value={qty} min={0} step={1}
                            onChange={e => handleQtyChange(i, parseInt(e.target.value) || 0)} />
                        </td>
                        <td className="pipe-fixed">{p.diameter}</td>
                        <td className="pipe-derived">{area}</td>
                        <td><button className="pipe-del-btn" onClick={() => handleDelete(i)}>×</button></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <button className="add-pipe-btn" onClick={() => setShowAddModal(true)}>＋ 新增管線</button>
          </div>
        </div>
      </div>

      {/* 分隔線 */}
      <div className="panel-divider" onMouseDown={onDividerMouseDown} />

      {/* 右側：計算結果 */}
      <div className="trunking-right">
        {/* 填充率儀表 */}
        <div className="data-table-wrap">
          <div className="data-table-head">
            <div className="data-table-title">填充率核算</div>
            <span className="badge badge-blue">線槽容納判定</span>
          </div>
          <div className="section-body" style={{ padding: '12px 16px' }}>
            {loading ? (
              <div style={{ textAlign: 'center', color: 'var(--text3)', padding: 16 }}>計算中...</div>
            ) : error ? (
              <div style={{ textAlign: 'center', color: 'var(--red)', padding: 16 }}>{error}</div>
            ) : (
              <div>
                {/* 填充率進度條 */}
                <div style={{ marginBottom: 16 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                    <span style={{ fontSize: 12, color: 'var(--text2)' }}>實際填充率</span>
                    <span style={{ fontSize: 18, fontWeight: 700, color: result?.resultStatus === 'ok' ? 'var(--green)' : 'var(--red)' }}>
                      {result ? (result.actualFillRatio * 100).toFixed(1) : 0}%
                    </span>
                  </div>
                  <div style={{ position: 'relative', height: 20, background: 'var(--bg2)', borderRadius: 10, overflow: 'hidden' }}>
                    <div style={{ position: 'absolute', left: `${fillRatio}%`, top: 0, bottom: 0, width: 2, background: 'var(--red)', opacity: 0.7 }} />
                    <div style={{
                      position: 'absolute',
                      left: 0, top: 0, bottom: 0,
                      width: `${Math.min((result?.actualFillRatio || 0) * 100, 100)}%`,
                      background: result?.resultStatus === 'ok'
                        ? 'linear-gradient(90deg, #22c55e, #4ade80)'
                        : 'linear-gradient(90deg, #ef4444, #f87171)',
                      borderRadius: 10,
                      transition: 'width 0.3s ease',
                    }} />
                  </div>
                </div>

                {/* 核算結論 */}
                <div className={`result-box ${result?.resultStatus || 'warn'}`}>
                  <div className="result-icon">{iconMap[result?.resultStatus || 'warn']}</div>
                  <div style={{ flex: 1 }}>
                    <div className="result-label">核算結論</div>
                    <div className="result-model">{result?.resultMessage || '請選擇線槽和管線'}</div>
                    {result?.selectedTrunking && (
                      <div className="result-note">
                        {result.selectedTrunking.model} · 截面積 {result.selectedTrunking.crossSection} mm²
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* 指標卡片 */}
        <div className="data-table-wrap">
          <div className="data-table-head"><div className="data-table-title">核算指標</div></div>
          <div style={{ padding: '10px 14px' }}>
            <div className="calc-grid">
              <div className="calc-item">
                <div className="calc-item-label">管線面積</div>
                <div><span className="calc-item-value">{result?.totalArea.toFixed(0) ?? '–'}</span><span className="calc-item-unit">mm²</span></div>
              </div>
              <div className="calc-item">
                <div className="calc-item-label">線槽面積</div>
                <div><span className="calc-item-value">{result?.selectedTrunking?.crossSection.toFixed(0) ?? '–'}</span><span className="calc-item-unit">mm²</span></div>
              </div>
              <div className="calc-item">
                <div className="calc-item-label">填充率</div>
                <div>
                  <span className="calc-item-value" style={{ color: result?.resultStatus === 'ok' ? 'var(--green)' : 'var(--red)' }}>
                    {result ? (result.actualFillRatio * 100).toFixed(1) : '–'}
                  </span>
                  <span className="calc-item-unit">%</span>
                </div>
              </div>
              <div className="calc-item">
                <div className="calc-item-label">最大直徑</div>
                <div><span className="calc-item-value">{result?.maxPipeDia.toFixed(1) ?? '–'}</span><span className="calc-item-unit">mm</span></div>
              </div>
              <div className="calc-item">
                <div className="calc-item-label">管線總數</div>
                <div><span className="calc-item-value">{result?.totalPipeCount ?? '–'}</span><span className="calc-item-unit">根</span></div>
              </div>
              <div className="calc-item">
                <div className="calc-item-label">容納判定</div>
                <div>
                  {result ? (
                    result.resultStatus === 'ok' ? (
                      <span className="status-ok">可容納</span>
                    ) : (
                      <span className="status-ng">超出限制</span>
                    )
                  ) : '–'}
                </div>
              </div>
            </div>
          </div>
        </div>

      </div>

      <PipeLibraryModal open={showLibModal} onClose={() => setShowLibModal(false)} pipeLib={pipeLib} onLoad={() => {}} />
      <AddFromLibModal
        open={showAddModal} onClose={() => setShowAddModal(false)}
        pipeLib={pipeLib} activePipes={activePipes}
        onConfirm={handleAddFromLib} />
    </div>
  );
};

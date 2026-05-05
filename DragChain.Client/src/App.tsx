import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { PipeType, ActivePipe, CalculationResponse } from './types';
import { pipeLibraryApi } from './api/pipeLibrary';
import { Header } from './components/Header';
import { BrandSelect } from './components/BrandSelect';
import { SensorConfig } from './components/SensorConfig';
import { PipeTable } from './components/PipeTable';
import { MotionParams } from './components/MotionParams';
import { MatchTable } from './components/MatchTable';
import { CalculationStepsPanel } from './components/CalculationStepsPanel';
import { CalcMetrics } from './components/CalcMetrics';
import { ResultPanel } from './components/ResultPanel';
import { WzlCatalogPage } from './components/WzlCatalogPage';
import { MeCatalogPage } from './components/MeCatalogPage';
import { PipeLibraryPage } from './components/PipeLibraryPage';
import { PipeLibraryModal } from './components/PipeLibraryModal';
import { AddFromLibModal } from './components/AddFromLibModal';
import { TrunkingCalcPage } from './components/TrunkingCalcPage';
import { TrunkingCatalogPage } from './components/TrunkingCatalogPage';

const DEFAULT_WZL_PIPES = [
  { libId: 1, qty: 1 }, { libId: 3, qty: 1 }, { libId: 4, qty: 2 },
  { libId: 5, qty: 5 }, { libId: 7, qty: 7 }, { libId: 12, qty: 2 }, { libId: 14, qty: 2 }
];
const DEFAULT_ME_PIPES = [
  { libId: 1, qty: 1 }, { libId: 2, qty: 1 }, { libId: 5, qty: 3 },
  { libId: 7, qty: 3 }, { libId: 12, qty: 3 }, { libId: 14, qty: 2 }
];
const DEFAULT_TRUNKING_PIPES = [
  { libId: 1, qty: 1 }, { libId: 3, qty: 1 }, { libId: 5, qty: 3 },
  { libId: 7, qty: 3 }, { libId: 12, qty: 3 }, { libId: 14, qty: 2 },
];

const MIN_LEFT_PX = 280;
const MAX_LEFT_PX = 700;

type Section = 'chain' | 'trunking' | 'pipe';

export default function App() {
  const [activeSection, setActiveSection] = useState<Section>('trunking');
  const [activeTab, setActiveTab] = useState('trunking');
  const [brand, setBrand] = useState('wzl');
  const [sensorCount, setSensorCount] = useState(15);
  const [magnetCount, setMagnetCount] = useState(0);
  const [motionType, setMotionType] = useState('横移');
  const [stroke, setStroke] = useState(1000);
  const [lmOffset, setLmOffset] = useState(50);
  const [activePipes, setActivePipes] = useState<ActivePipe[]>(DEFAULT_WZL_PIPES);
  const [pipeLib, setPipeLib] = useState<PipeType[]>([]);
  const [showLibModal, setShowLibModal] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  const [calcResult, setCalcResult] = useState<CalculationResponse | null>(null);
  const [calcLoading, setCalcLoading] = useState(false);
  const [calcError, setCalcError] = useState<string | null>(null);

  const [trunkingPipes, setTrunkingPipes] = useState<ActivePipe[]>(DEFAULT_TRUNKING_PIPES);

  const [leftWidth, setLeftWidth] = useState(420);
  const isDragging = useRef(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const loadPipeLib = useCallback(() => {
    pipeLibraryApi.getAll().then(setPipeLib).catch(console.error);
  }, []);

  useEffect(() => { loadPipeLib(); }, [loadPipeLib]);

  const handleBrandChange = (b: string) => {
    setBrand(b);
    setActivePipes(b === 'wzl' ? DEFAULT_WZL_PIPES : DEFAULT_ME_PIPES);
    if (b === 'wzl') { setSensorCount(15); setStroke(1000); }
    else { setSensorCount(4); setStroke(2300); }
    setCalcResult(null);
  };

  const coreCount = sensorCount + Math.ceil(sensorCount / 3) * 2 + Math.ceil(magnetCount / 3) * 2 + 2;
  const needSpan = motionType === '横移' ? stroke / 2 : 0;

  const pipeMap = pipeLib.reduce((acc, p) => { acc[p.id] = p; return acc; }, {} as Record<number, PipeType>);
  let maxBend = 0, maxBendDia = 0;
  activePipes.forEach(ap => {
    const p = pipeMap[ap.libId];
    if (!p || !ap.qty) return;
    const b = p.diameter * p.bendMultiplier;
    if (b > maxBend) { maxBend = b; maxBendDia = p.diameter; }
  });

  const triggerCalc = useCallback(async () => {
    setCalcLoading(true);
    setCalcError(null);
    try {
      const req = {
        brand: brand as 'wzl' | 'me',
        sensorCount,
        magnetCount,
        motionType: motionType as '横移' | '升降',
        stroke,
        lmOffset,
        pipes: activePipes.map(ap => ({ pipeTypeId: ap.libId, qty: ap.qty })),
      };
      const res = await fetch(`${import.meta.env.VITE_API_BASE || 'http://localhost:5256'}/api/Calculation/calc`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(req),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data: CalculationResponse = await res.json();
      setCalcResult(data);
    } catch (e: unknown) {
      setCalcError(e instanceof Error ? e.message : '計算失敗');
    } finally {
      setCalcLoading(false);
    }
  }, [brand, sensorCount, magnetCount, motionType, stroke, lmOffset, activePipes]);

  useEffect(() => {
    const timer = setTimeout(triggerCalc, 300);
    return () => clearTimeout(timer);
  }, [triggerCalc]);

  const handleAddFromLib = (ids: number[]) => {
    setActivePipes(prev => {
      const existing = new Set(prev.map(p => p.libId));
      const added = ids.filter(id => !existing.has(id)).map(id => ({ libId: id, qty: 1 }));
      return [...prev, ...added];
    });
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

  const handleSectionChange = (s: Section) => {
    setActiveSection(s);
    if (s === 'chain')    setActiveTab('calc');
    if (s === 'trunking') setActiveTab('trunking');
    if (s === 'pipe')      setActiveTab('pipe');
  };

  return (
    <>
      <Header
        activeSection={activeSection}
        activeTab={activeTab}
        onSectionChange={handleSectionChange}
        onTabChange={setActiveTab}
      />

      {/* 拖鏈計算 Tab */}
      {activeSection === 'chain' && activeTab === 'calc' && (
        <div className="main-wrap" ref={containerRef}>
          <div className="left-panel" style={{ width: leftWidth, minWidth: leftWidth, flexShrink: 0 }}>
            <BrandSelect brand={brand} onChange={handleBrandChange} />
            <SensorConfig
              sensorCount={sensorCount} magnetCount={magnetCount} coreCount={coreCount}
              onSensorChange={v => setSensorCount(v)} onMagnetChange={v => setMagnetCount(v)} />
            <PipeTable
              pipeLib={pipeLib} activePipes={activePipes}
              onQtyChange={(i, q) => setActivePipes(prev => prev.map((p, idx) => idx === i ? { ...p, qty: q } : p))}
              onDelete={i => setActivePipes(prev => prev.filter((_, idx) => idx !== i))}
              onAddFromLib={() => setShowAddModal(true)}
              maxBend={maxBend} maxBendDia={maxBendDia} />
            <MotionParams
              motionType={motionType} stroke={stroke} lmOffset={lmOffset}
              needSpan={needSpan}
              onMotionChange={setMotionType} onStrokeChange={setStroke} onLmChange={setLmOffset} />
          </div>
          <div className="panel-divider" onMouseDown={onDividerMouseDown} />
          <div className="right-panel">
            <div className="page-section active">
              <MatchTable brand={brand} results={calcResult?.matchResults || []} />
              <CalculationStepsPanel
                steps={calcResult?.steps || {
                  step3_1_MinHeight: '–', step3_2_BendTube: '–', step3_2_BendCable: '–',
                  step3_2_BendMax: '–', step3_3_AreaSum: '–', step3_3_Ratio: '–',
                  step3_3_MinArea: '–', step3_4_PrelimModel: '–', step3_5_Motion: '–',
                  step3_5_Stroke: '–', step3_5_Lm: '–', step3_5_PrelimLp: '–',
                  step3_5_PrelimLk: '–', step3_5_PrelimFull: '–', step3_6_NeedSpan: '–',
                  step3_6_Load: '–', step3_6_SpanOk: '–', step3_6_FinalModel: '–',
                  step3_6_FinalLp: '–', step3_6_FinalLk: '–',
                }}
                tubeBend={calcResult?.tubeBend || 0}
                cableBend={calcResult?.cableBend || 0}
                encoderBend={calcResult?.encoderBend || 0}
                maxBend={maxBend}
              />
              <div style={{ display: 'grid', gridTemplateColumns: '1fr minmax(280px, 340px)', gap: 12, alignItems: 'start' }}>
                <CalcMetrics result={calcResult} />
                <ResultPanel result={calcResult} loading={calcLoading} error={calcError} />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 拖鏈 WZL / ME Tabs — 全高自帶分欄 */}
      {activeSection === 'chain' && (activeTab === 'wzl' || activeTab === 'me') && (
        <div className="main-wrap">
          {activeTab === 'wzl' && <WzlCatalogPage />}
          {activeTab === 'me' && <MeCatalogPage />}
        </div>
      )}

      {/* 線槽頁面 */}
      {activeSection === 'trunking' && (
        <div className="main-wrap">
          {activeTab === 'trunking' && (
            <TrunkingCalcPage
              pipeLib={pipeLib}
              activePipes={trunkingPipes}
              onPipesChange={setTrunkingPipes}
            />
          )}
          {activeTab === 'trunking-catalog' && <TrunkingCatalogPage />}
        </div>
      )}

      {/* 管線庫頁面 */}
      {activeSection === 'pipe' && <div className="main-wrap"><PipeLibraryPage /></div>}

      <PipeLibraryModal open={showLibModal} onClose={() => setShowLibModal(false)} pipeLib={pipeLib} onLoad={loadPipeLib} />
      <AddFromLibModal
        open={showAddModal} onClose={() => setShowAddModal(false)}
        pipeLib={pipeLib} activePipes={activePipes}
        onConfirm={handleAddFromLib} />
    </>
  );
}

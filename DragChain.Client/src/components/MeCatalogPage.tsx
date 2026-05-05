import React, { useState, useEffect } from 'react';
import { meApi, type CreateMeCatalog } from '../api/catalog';
import type { MeCatalog } from '../types';
import { ConfirmModal } from './ConfirmModal';

interface EditFormData {
  baseModel: string;
  innerHeight: string;
  innerWidth: string;
  r1: string;
  r2: string;
  r3: string;
  r1Suffix: string;
  r2Suffix: string;
  r3Suffix: string;
  lp1: string;
  lp2: string;
  lp3: string;
  innerArea: string;
  maxWeight: string;
  spanBase: string;
  spanSlope: string;
}

const EMPTY_FORM: EditFormData = {
  baseModel: '', innerHeight: '', innerWidth: '',
  r1: '', r2: '', r3: '',
  r1Suffix: '', r2Suffix: '', r3Suffix: '',
  lp1: '', lp2: '', lp3: '',
  innerArea: '', maxWeight: '', spanBase: '', spanSlope: '',
};

export const MeCatalogPage: React.FC = () => {
  const [data, setData] = useState<MeCatalog[]>([]);
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [newItem, setNewItem] = useState<EditFormData>(EMPTY_FORM);
  const [addError, setAddError] = useState('');

  const [editTarget, setEditTarget] = useState<MeCatalog | null>(null);
  const [editForm, setEditForm] = useState<EditFormData>(EMPTY_FORM);
  const [editError, setEditError] = useState('');
  const [confirmId, setConfirmId] = useState<number | null>(null);

  const load = () => meApi.getAll().then(setData).catch(console.error);
  useEffect(() => { load(); }, []);

  const openEdit = (r: MeCatalog) => {
    setEditTarget(r);
    setEditForm({
      baseModel: r.baseModel,
      innerHeight: String(r.innerHeight),
      innerWidth: String(r.innerWidth),
      r1: String(r.r1),
      r2: String(r.r2),
      r3: String(r.r3),
      r1Suffix: r.r1Suffix || '',
      r2Suffix: r.r2Suffix || '',
      r3Suffix: r.r3Suffix || '',
      lp1: String(r.lp1 || 0),
      lp2: String(r.lp2 || 0),
      lp3: String(r.lp3 || 0),
      innerArea: String(r.innerArea),
      maxWeight: String(r.maxWeight || 0),
      spanBase: String(r.spanBase || 0),
      spanSlope: String(r.spanSlope || 0),
    });
    setEditError('');
  };

  const closeEdit = () => {
    setEditTarget(null);
    setEditError('');
  };

  const handleEditSave = () => {
    if (!editTarget) return;
    if (!editForm.baseModel.trim()) { setEditError('型號基礎不能為空'); return; }
    const innerHeight = parseFloat(editForm.innerHeight);
    const innerWidth = parseFloat(editForm.innerWidth);
    const r1 = parseFloat(editForm.r1);
    const r2 = parseFloat(editForm.r2);
    const r3 = parseFloat(editForm.r3);
    const innerArea = parseFloat(editForm.innerArea);
    if (isNaN(innerHeight) || innerHeight <= 0) { setEditError('內高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setEditError('內寬必須為正數'); return; }
    if (isNaN(r1) || r1 <= 0) { setEditError('R小必須為正數'); return; }
    if (isNaN(r2) || r2 <= 0) { setEditError('R中必須為正數'); return; }
    if (isNaN(r3) || r3 <= 0) { setEditError('R大必須為正數'); return; }
    if (isNaN(innerArea) || innerArea <= 0) { setEditError('內部面積必須為正數'); return; }
    setEditError('');
    meApi.update(editTarget.id, {
      baseModel: editForm.baseModel.trim(),
      innerHeight,
      innerWidth,
      r1, r2, r3,
      r1Suffix: editForm.r1Suffix.trim(),
      r2Suffix: editForm.r2Suffix.trim(),
      r3Suffix: editForm.r3Suffix.trim(),
      lp1: editForm.lp1 ? parseFloat(editForm.lp1) : 0,
      lp2: editForm.lp2 ? parseFloat(editForm.lp2) : 0,
      lp3: editForm.lp3 ? parseFloat(editForm.lp3) : 0,
      innerArea,
      maxWeight: editForm.maxWeight ? parseFloat(editForm.maxWeight) : 0,
      spanBase: editForm.spanBase ? parseFloat(editForm.spanBase) : 0,
      spanSlope: editForm.spanSlope ? parseFloat(editForm.spanSlope) : 0,
    }).then(() => { closeEdit(); load(); }).catch((e: Error) => setEditError(e.message || '保存失敗'));
  };

  const handleDelete = (id: number) => setConfirmId(id);

  const handleAdd = () => {
    if (!newItem.baseModel.trim()) { setAddError('型號基礎不能為空'); return; }
    const innerHeight = parseFloat(newItem.innerHeight);
    const innerWidth = parseFloat(newItem.innerWidth);
    const r1 = parseFloat(newItem.r1);
    const r2 = parseFloat(newItem.r2);
    const r3 = parseFloat(newItem.r3);
    const innerArea = parseFloat(newItem.innerArea);
    if (isNaN(innerHeight) || innerHeight <= 0) { setAddError('內高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setAddError('內寬必須為正數'); return; }
    if (isNaN(r1) || r1 <= 0) { setAddError('R小必須為正數'); return; }
    if (isNaN(r2) || r2 <= 0) { setAddError('R中必須為正數'); return; }
    if (isNaN(r3) || r3 <= 0) { setAddError('R大必須為正數'); return; }
    if (isNaN(innerArea) || innerArea <= 0) { setAddError('內部面積必須為正數'); return; }
    setAddError('');
    meApi.create({
      baseModel: newItem.baseModel.trim(),
      innerHeight, innerWidth, r1, r2, r3,
      r1Suffix: newItem.r1Suffix.trim(),
      r2Suffix: newItem.r2Suffix.trim(),
      r3Suffix: newItem.r3Suffix.trim(),
      lp1: newItem.lp1 ? parseFloat(newItem.lp1) : 0,
      lp2: newItem.lp2 ? parseFloat(newItem.lp2) : 0,
      lp3: newItem.lp3 ? parseFloat(newItem.lp3) : 0,
      innerArea,
      maxWeight: newItem.maxWeight ? parseFloat(newItem.maxWeight) : 0,
      spanBase: newItem.spanBase ? parseFloat(newItem.spanBase) : 0,
      spanSlope: newItem.spanSlope ? parseFloat(newItem.spanSlope) : 0,
    }).then(() => {
      setShowAddForm(false);
      setNewItem(EMPTY_FORM);
      load();
    }).catch((e: Error) => setAddError(e.message || '新增失敗'));
  };

  const filters = ['all', 'ME15', 'ME20', 'ME25', 'ME35'];
  const filtered = data.filter(r => {
    const matchFilter = filter === 'all' || r.baseModel.startsWith(filter);
    const matchSearch = !search || r.baseModel.toLowerCase().includes(search.toLowerCase());
    return matchFilter && matchSearch;
  });

  return (
    <>
      <div className="data-table-wrap" style={{ flex: 1, display: 'flex', flexDirection: 'column', margin: 'var(--space-2)', overflow: 'hidden' }}>
        {/* 頂部標題欄 */}
        <div className="lib-topbar">
          <div className="lib-topbar-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
              <line x1="3" y1="9" x2="21" y2="9"/>
              <line x1="9" y1="21" x2="9" y2="9"/>
            </svg>
            <span>犸幕 ME 型錄</span>
            <span className="lib-count-badge">{filtered.length}</span>
          </div>
          <button className="btn-primary-sm" onClick={() => setShowAddForm(v => !v)}>
            {showAddForm ? (
              <>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                  <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                </svg>
                取消
              </>
            ) : (
              <>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                  <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                </svg>
                新增 ME
              </>
            )}
          </button>
        </div>

        {/* 新增表單 */}
        {showAddForm && (
          <div className="lib-add-panel">
            <div className="lib-panel-header">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="16"/>
                <line x1="8" y1="12" x2="16" y2="12"/>
              </svg>
              新增 ME 型號
            </div>
            <div className="lib-add-grid">
              <div className="lib-form-field">
                <label>型號基礎 <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 160 }} placeholder="如：ME20.40.R"
                  value={newItem.baseModel} onChange={e => setNewItem(v => ({ ...v, baseModel: e.target.value }))} autoFocus />
              </div>
              <div className="lib-form-field">
                <label>內高 A mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 80 }} type="number" min="0"
                  value={newItem.innerHeight} onChange={e => setNewItem(v => ({ ...v, innerHeight: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>內寬 B mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 80 }} type="number" min="0"
                  value={newItem.innerWidth} onChange={e => setNewItem(v => ({ ...v, innerWidth: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>R小 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 70 }} type="number" min="0"
                  value={newItem.r1} onChange={e => setNewItem(v => ({ ...v, r1: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>R中 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 70 }} type="number" min="0"
                  value={newItem.r2} onChange={e => setNewItem(v => ({ ...v, r2: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>R大 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 70 }} type="number" min="0"
                  value={newItem.r3} onChange={e => setNewItem(v => ({ ...v, r3: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>內部面積 mm² <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 100 }} type="number" min="0"
                  value={newItem.innerArea} onChange={e => setNewItem(v => ({ ...v, innerArea: e.target.value }))} />
              </div>
              <div className="lib-form-field" style={{ justifyContent: 'flex-end' }}>
                <button className="btn-primary-sm" onClick={handleAdd}>
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                    <polyline points="20 6 9 17 4 12"/>
                  </svg>
                  保存
                </button>
              </div>
            </div>
            {addError && (
              <div className="lib-error-msg">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                {addError}
              </div>
            )}
          </div>
        )}

        {/* 篩選欄 */}
        <div className="catalog-filter">
          {filters.map(f => (
            <button key={f} className={`filter-btn ${filter === f ? 'active' : ''}`} onClick={() => setFilter(f)}>
              {f === 'all' ? '全部' : f}
            </button>
          ))}
          <div className="lib-search-wrap">
            <svg className="lib-search-icon" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            <input className="search-input lib-search" placeholder="搜尋型號…"
              value={search} onChange={e => setSearch(e.target.value)} />
          </div>
        </div>

        {/* 數據表格 */}
        <div className="tbl-scroll">
          <table className="data-tbl">
            <thead>
              <tr>
                <th>型號</th><th>內高A</th><th>內寬B</th>
                <th>R小</th><th>R中</th><th>R大</th>
                <th>內部面積</th>
                <th style={{ minWidth: 100 }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(r => (
                <tr key={r.id}>
                  <td><span className="lib-name-cell">{r.baseModel}</span></td>
                  <td>{r.innerHeight}</td>
                  <td>{r.innerWidth}</td>
                  <td>{r.r1}</td>
                  <td>{r.r2}</td>
                  <td>{r.r3}</td>
                  <td>{r.innerArea}</td>
                  <td>
                    <div className="lib-action-group">
                      <button className="lib-action-btn lib-action-edit" onClick={() => openEdit(r)}>
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                        </svg>
                        編輯
                      </button>
                      <button className="lib-action-btn lib-action-del" onClick={() => handleDelete(r.id)}>
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <polyline points="3 6 5 6 21 6"/>
                          <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                          <path d="M10 11v6"/><path d="M14 11v6"/>
                        </svg>
                        刪除
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={8}>
                    <div className="lib-empty">
                      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                        <line x1="3" y1="9" x2="21" y2="9"/>
                      </svg>
                      <span>沒有找到匹配的記錄</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="record-count">
          <span>共 {filtered.length} 筆記錄</span>
        </div>
      </div>

      {/* 編輯模態框 */}
      {editTarget && (
        <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) closeEdit(); }}>
          <div className="modal-box lib-edit-modal" style={{ width: 700 }}>
            <div className="modal-head">
              <div className="lib-modal-title">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
                編輯 ME
                <span className="lib-modal-id">ID: {editTarget.id}</span>
              </div>
              <button className="modal-close" onClick={closeEdit}>
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                  <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                </svg>
              </button>
            </div>
            <div className="modal-body">
              <div className="lib-edit-grid">
                <div className="lib-form-field">
                  <label>型號基礎 <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="ME20.40.R"
                    value={editForm.baseModel} onChange={e => setEditForm(v => ({ ...v, baseModel: e.target.value }))} autoFocus />
                </div>
                <div className="lib-form-field">
                  <label>內高 A mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.innerHeight} onChange={e => setEditForm(v => ({ ...v, innerHeight: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>內寬 B mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.innerWidth} onChange={e => setEditForm(v => ({ ...v, innerWidth: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>R小 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.r1} onChange={e => setEditForm(v => ({ ...v, r1: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>R中 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.r2} onChange={e => setEditForm(v => ({ ...v, r2: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>R大 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.r3} onChange={e => setEditForm(v => ({ ...v, r3: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>後綴（小/中/大）</label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="28/38/48"
                    value={`${editForm.r1Suffix}/${editForm.r2Suffix}/${editForm.r3Suffix}`}
                    onChange={e => {
                      const parts = e.target.value.split('/').map(s => s.trim());
                      setEditForm(v => ({
                        ...v,
                        r1Suffix: parts[0] || '',
                        r2Suffix: parts[1] || '',
                        r3Suffix: parts[2] || '',
                      }));
                    }} />
                </div>
                <div className="lib-form-field">
                  <label>內部面積 mm² <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.innerArea} onChange={e => setEditForm(v => ({ ...v, innerArea: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>Lp@R小 mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.lp1} onChange={e => setEditForm(v => ({ ...v, lp1: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>Lp@R中 mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.lp2} onChange={e => setEditForm(v => ({ ...v, lp2: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>Lp@R大 mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.lp3} onChange={e => setEditForm(v => ({ ...v, lp3: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>額定承重 kg/m</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" step="0.1" min="0"
                    value={editForm.maxWeight} onChange={e => setEditForm(v => ({ ...v, maxWeight: e.target.value }))} />
                </div>
              </div>
              {editError && (
                <div className="lib-error-msg">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                  {editError}
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={closeEdit}>取消</button>
              <button className="btn btn-primary" onClick={handleEditSave}>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                  <polyline points="20 6 9 17 4 12"/>
                </svg>
                保存修改
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmModal
        isOpen={confirmId !== null}
        title="確認刪除"
        message={`確定要刪除型號「${data.find(r => r.id === confirmId)?.baseModel ?? ''}」嗎？此操作不可撤銷。`}
        confirmLabel="刪除"
        confirmDanger
        onConfirm={() => {
          if (confirmId !== null) { meApi.delete(confirmId).then(load).catch(console.error); setConfirmId(null); }
        }}
        onCancel={() => setConfirmId(null)}
      />
    </>
  );
};

import React, { useState, useEffect } from 'react';
import { wzlApi, type CreateWzlCatalog } from '../api/catalog';
import type { WzlCatalog } from '../types';
import { ConfirmModal } from './ConfirmModal';

interface EditFormData {
  model: string;
  function: string;
  stroke: string;
  innerHeight: string;
  innerWidth: string;
  outerHeight: string;
  outerWidth: string;
  minRadius: string;
  recRadius: string;
  reservedK: string;
  bendLength: string;
  mountingH1: string;
  interferenceH2: string;
  innerArea: string;
  appPipes: string;
}

const EMPTY_FORM: EditFormData = {
  model: '', function: '', stroke: '',
  innerHeight: '', innerWidth: '', outerHeight: '', outerWidth: '',
  minRadius: '', recRadius: '', reservedK: '', bendLength: '',
  mountingH1: '', interferenceH2: '', innerArea: '', appPipes: '',
};

export const WzlCatalogPage: React.FC = () => {
  const [data, setData] = useState<WzlCatalog[]>([]);
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [newItem, setNewItem] = useState<EditFormData>(EMPTY_FORM);
  const [addError, setAddError] = useState('');

  const [editTarget, setEditTarget] = useState<WzlCatalog | null>(null);
  const [editForm, setEditForm] = useState<EditFormData>(EMPTY_FORM);
  const [editError, setEditError] = useState('');
  const [confirmId, setConfirmId] = useState<number | null>(null);

  const load = () => wzlApi.getAll().then(setData).catch(console.error);
  useEffect(() => { load(); }, []);

  const openEdit = (r: WzlCatalog) => {
    setEditTarget(r);
    setEditForm({
      model: r.model,
      function: r.function || '',
      stroke: r.stroke || '',
      innerHeight: String(r.innerHeight),
      innerWidth: String(r.innerWidth),
      outerHeight: String(r.outerHeight || 0),
      outerWidth: String(r.outerWidth || 0),
      minRadius: String(r.minRadius || 0),
      recRadius: String(r.recRadius),
      reservedK: String(r.reservedK || 0),
      bendLength: String(r.bendLength),
      mountingH1: r.mountingH1 || '',
      interferenceH2: r.interferenceH2 || '',
      innerArea: r.innerArea != null ? String(r.innerArea) : '',
      appPipes: r.appPipes || '',
    });
    setEditError('');
  };

  const closeEdit = () => {
    setEditTarget(null);
    setEditError('');
  };

  const handleEditSave = () => {
    if (!editTarget) return;
    if (!editForm.model.trim()) { setEditError('型號不能為空'); return; }
    if (!editForm.function.trim()) { setEditError('功能選擇不能為空'); return; }
    const innerHeight = parseFloat(editForm.innerHeight);
    const innerWidth = parseFloat(editForm.innerWidth);
    const recRadius = parseFloat(editForm.recRadius);
    const bendLength = parseFloat(editForm.bendLength);
    if (isNaN(innerHeight) || innerHeight <= 0) { setEditError('內高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setEditError('內寬必須為正數'); return; }
    if (isNaN(recRadius) || recRadius <= 0) { setEditError('推薦半徑必須為正數'); return; }
    if (isNaN(bendLength) || bendLength <= 0) { setEditError('彎曲長度必須為正數'); return; }
    setEditError('');
    wzlApi.update(editTarget.id, {
      model: editForm.model.trim(),
      function: editForm.function.trim(),
      stroke: editForm.stroke.trim(),
      innerHeight,
      innerWidth,
      outerHeight: editForm.outerHeight ? parseFloat(editForm.outerHeight) : undefined,
      outerWidth: editForm.outerWidth ? parseFloat(editForm.outerWidth) : undefined,
      minRadius: editForm.minRadius ? parseFloat(editForm.minRadius) : undefined,
      reservedK: editForm.reservedK ? parseFloat(editForm.reservedK) : undefined,
      recRadius,
      bendLength,
      mountingH1: editForm.mountingH1.trim() || undefined,
      interferenceH2: editForm.interferenceH2.trim() || undefined,
      innerArea: editForm.innerArea ? parseFloat(editForm.innerArea) : undefined,
      appPipes: editForm.appPipes.trim() || undefined,
    }).then(() => { closeEdit(); load(); }).catch((e: Error) => setEditError(e.message || '保存失敗'));
  };

  const handleDelete = (id: number) => setConfirmId(id);

  const handleAdd = () => {
    if (!newItem.model.trim()) { setAddError('型號不能為空'); return; }
    if (!newItem.function.trim()) { setAddError('功能選擇不能為空'); return; }
    const innerHeight = parseFloat(newItem.innerHeight);
    const innerWidth = parseFloat(newItem.innerWidth);
    const recRadius = parseFloat(newItem.recRadius);
    const bendLength = parseFloat(newItem.bendLength);
    if (isNaN(innerHeight) || innerHeight <= 0) { setAddError('內高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setAddError('內寬必須為正數'); return; }
    if (isNaN(recRadius) || recRadius <= 0) { setAddError('推薦半徑必須為正數'); return; }
    if (isNaN(bendLength) || bendLength <= 0) { setAddError('彎曲長度必須為正數'); return; }
    setAddError('');
    wzlApi.create({
      model: newItem.model.trim(),
      function: newItem.function.trim(),
      stroke: newItem.stroke.trim(),
      innerHeight, innerWidth, recRadius, bendLength,
      outerHeight: newItem.outerHeight ? parseFloat(newItem.outerHeight) : 0,
      outerWidth: newItem.outerWidth ? parseFloat(newItem.outerWidth) : 0,
      minRadius: newItem.minRadius ? parseFloat(newItem.minRadius) : 0,
      reservedK: newItem.reservedK ? parseFloat(newItem.reservedK) : 0,
      mountingH1: newItem.mountingH1.trim(),
      interferenceH2: newItem.interferenceH2.trim(),
      innerArea: newItem.innerArea ? parseFloat(newItem.innerArea) : null,
      appPipes: newItem.appPipes.trim(),
    }).then(() => {
      setShowAddForm(false);
      setNewItem(EMPTY_FORM);
      load();
    }).catch((e: Error) => setAddError(e.message || '新增失敗'));
  };

  const filters = ['all', 'WZL15', 'WZL18', 'WZL22', 'WZL28', 'WZL35', 'WZL40'];
  const filtered = data.filter(r => {
    const matchFilter = filter === 'all' || r.model.startsWith(filter);
    const matchSearch = !search || r.model.toLowerCase().includes(search.toLowerCase()) || (r.function || '').toLowerCase().includes(search.toLowerCase());
    return matchFilter && matchSearch;
  });

  const fi = (field: keyof EditFormData) => ({
    value: (f: EditFormData) => f[field],
    onChange: (setter: React.Dispatch<React.SetStateAction<EditFormData>>) => (e: React.ChangeEvent<HTMLInputElement>) => setter((prev: EditFormData) => ({ ...prev, [field]: e.target.value })),
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
            <span>沃德 WZL 型錄</span>
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
                新增 WZL
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
              新增 WZL 型號
            </div>
            <div className="lib-add-grid">
              <div className="lib-form-field">
                <label>型號 <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 160 }} placeholder="如：WZL15.025.02"
                  value={newItem.model} onChange={e => setNewItem(v => ({ ...v, model: e.target.value }))} autoFocus />
              </div>
              <div className="lib-form-field">
                <label>功能選擇 <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 120 }} placeholder="S:標準款"
                  value={newItem.function} onChange={e => setNewItem(v => ({ ...v, function: e.target.value }))} />
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
                <label>推薦 R mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 80 }} type="number" min="0"
                  value={newItem.recRadius} onChange={e => setNewItem(v => ({ ...v, recRadius: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>彎曲 Lp mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 80 }} type="number" min="0"
                  value={newItem.bendLength} onChange={e => setNewItem(v => ({ ...v, bendLength: e.target.value }))} />
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
            <input className="search-input lib-search" placeholder="搜尋型號、功能…"
              value={search} onChange={e => setSearch(e.target.value)} />
          </div>
        </div>

        {/* 數據表格 */}
        <div className="tbl-scroll">
          <table className="data-tbl">
            <thead>
              <tr>
                <th>型號</th><th>功能</th><th>內高A</th><th>內寬B</th>
                <th>最小R</th><th>推薦R</th><th>彎曲Lp</th>
                <th style={{ minWidth: 100 }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(r => (
                <tr key={r.id}>
                  <td><span className="lib-name-cell">{r.model}</span></td>
                  <td>{r.function || '–'}</td>
                  <td>{r.innerHeight}</td>
                  <td>{r.innerWidth}</td>
                  <td>{r.minRadius || '–'}</td>
                  <td>{r.recRadius}</td>
                  <td>{r.bendLength}</td>
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
                編輯 WZL
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
                  <label>型號 <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="WZL15.025.02"
                    value={editForm.model} onChange={e => setEditForm(v => ({ ...v, model: e.target.value }))} autoFocus />
                </div>
                <div className="lib-form-field">
                  <label>功能選擇 <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="S:標準款"
                    value={editForm.function} onChange={e => setEditForm(v => ({ ...v, function: e.target.value }))} />
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
                  <label>外高 C mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.outerHeight} onChange={e => setEditForm(v => ({ ...v, outerHeight: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>外寬 D mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.outerWidth} onChange={e => setEditForm(v => ({ ...v, outerWidth: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>最小 R mm</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.minRadius} onChange={e => setEditForm(v => ({ ...v, minRadius: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>推薦 R mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.recRadius} onChange={e => setEditForm(v => ({ ...v, recRadius: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>預留 K</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.reservedK} onChange={e => setEditForm(v => ({ ...v, reservedK: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>彎曲 Lp mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.bendLength} onChange={e => setEditForm(v => ({ ...v, bendLength: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>適用行程</label>
                  <input className="lib-input" style={{ width: '100%' }}
                    value={editForm.stroke} onChange={e => setEditForm(v => ({ ...v, stroke: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>內部面積 mm²</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" step="0.1" min="0"
                    value={editForm.innerArea} onChange={e => setEditForm(v => ({ ...v, innerArea: e.target.value }))} />
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
        message={`確定要刪除「${data.find(r => r.id === confirmId)?.model ?? ''} ${data.find(r => r.id === confirmId)?.function ?? ''}」嗎？此操作不可撤銷。`}
        confirmLabel="刪除"
        confirmDanger
        onConfirm={() => {
          if (confirmId !== null) { wzlApi.delete(confirmId).then(load).catch(console.error); setConfirmId(null); }
        }}
        onCancel={() => setConfirmId(null)}
      />
    </>
  );
};

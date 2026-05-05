import React, { useState, useEffect } from 'react';
import { trunkingApi, type CreateTrunkingDto } from '../api/trunking';
import { ConfirmModal } from './ConfirmModal';

interface TrunkingType {
  id: number;
  model: string;
  width: number;
  height: number;
  innerWidth: number;
  innerHeight: number;
  crossSection: number;
  material: string;
  remarks: string;
}

interface EditFormData {
  model: string;
  width: string;
  height: string;
  innerWidth: string;
  innerHeight: string;
  crossSection: string;
  material: string;
  remarks: string;
}

export const TrunkingCatalogPage: React.FC = () => {
  const [data, setData] = useState<TrunkingType[]>([]);
  const [search, setSearch] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [newItem, setNewItem] = useState<EditFormData>({
    model: '', width: '', height: '', innerWidth: '', innerHeight: '',
    crossSection: '', material: '', remarks: '',
  });
  const [addError, setAddError] = useState('');

  // 编辑模态框状态
  const [editTarget, setEditTarget] = useState<TrunkingType | null>(null);
  const [editForm, setEditForm] = useState<EditFormData>({
    model: '', width: '', height: '', innerWidth: '', innerHeight: '',
    crossSection: '', material: '', remarks: '',
  });
  const [editError, setEditError] = useState('');
  const [confirmId, setConfirmId] = useState<number | null>(null);

  const load = () => trunkingApi.getAll().then(setData).catch(console.error);
  useEffect(() => { load(); }, []);

  const openEdit = (r: TrunkingType) => {
    setEditTarget(r);
    setEditForm({
      model: r.model,
      width: String(r.width),
      height: String(r.height),
      innerWidth: String(r.innerWidth),
      innerHeight: String(r.innerHeight),
      crossSection: String(r.crossSection),
      material: r.material,
      remarks: r.remarks,
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
    const width = parseFloat(editForm.width);
    const height = parseFloat(editForm.height);
    const innerWidth = parseFloat(editForm.innerWidth);
    const innerHeight = parseFloat(editForm.innerHeight);
    const crossSection = parseFloat(editForm.crossSection);
    if (isNaN(width) || width <= 0) { setEditError('外寬必須為正數'); return; }
    if (isNaN(height) || height <= 0) { setEditError('外高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setEditError('內寬必須為正數'); return; }
    if (isNaN(innerHeight) || innerHeight <= 0) { setEditError('內高必須為正數'); return; }
    setEditError('');
    trunkingApi.update(editTarget.id, {
      model: editForm.model.trim(),
      width,
      height,
      innerWidth,
      innerHeight,
      crossSection: isNaN(crossSection) ? 0 : crossSection,
      material: editForm.material.trim(),
      remarks: editForm.remarks.trim(),
    }).then(() => {
      closeEdit();
      load();
    }).catch((e: Error) => setEditError(e.message || '保存失敗'));
  };

  const handleDelete = (id: number) => {
    setConfirmId(id);
  };

  const handleAdd = () => {
    if (!newItem.model.trim()) { setAddError('型號不能為空'); return; }
    const width = parseFloat(newItem.width);
    const height = parseFloat(newItem.height);
    const innerWidth = parseFloat(newItem.innerWidth);
    const innerHeight = parseFloat(newItem.innerHeight);
    const crossSection = parseFloat(newItem.crossSection);
    if (isNaN(width) || width <= 0) { setAddError('外寬必須為正數'); return; }
    if (isNaN(height) || height <= 0) { setAddError('外高必須為正數'); return; }
    if (isNaN(innerWidth) || innerWidth <= 0) { setAddError('內寬必須為正數'); return; }
    if (isNaN(innerHeight) || innerHeight <= 0) { setAddError('內高必須為正數'); return; }
    setAddError('');
    trunkingApi.create({
      model: newItem.model.trim(),
      width,
      height,
      innerWidth,
      innerHeight,
      crossSection: isNaN(crossSection) ? 0 : crossSection,
      material: newItem.material.trim(),
      remarks: newItem.remarks.trim(),
    }).then(() => {
      setShowAddForm(false);
      setNewItem({ model: '', width: '', height: '', innerWidth: '', innerHeight: '', crossSection: '', material: '', remarks: '' });
      load();
    }).catch((e: Error) => setAddError(e.message || '新增失敗'));
  };

  const filtered = data.filter(r =>
    !search ||
    r.model.toLowerCase().includes(search.toLowerCase()) ||
    r.material.toLowerCase().includes(search.toLowerCase())
  );

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
            <span>線槽庫維護</span>
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
                新增線槽
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
              新增線槽型號
            </div>
            <div className="lib-add-grid">
              <div className="lib-form-field">
                <label>型號 <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 160 }} placeholder="如：TK-25×25"
                  value={newItem.model} onChange={e => setNewItem(v => ({ ...v, model: e.target.value }))} autoFocus />
              </div>
              <div className="lib-form-field">
                <label>外寬 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 90 }} type="number" min="0" placeholder="30"
                  value={newItem.width} onChange={e => setNewItem(v => ({ ...v, width: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>外高 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 90 }} type="number" min="0" placeholder="30"
                  value={newItem.height} onChange={e => setNewItem(v => ({ ...v, height: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>內寬 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 90 }} type="number" min="0" placeholder="25"
                  value={newItem.innerWidth} onChange={e => setNewItem(v => ({ ...v, innerWidth: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>內高 mm <span className="required-mark">*</span></label>
                <input className="lib-input" style={{ width: 90 }} type="number" min="0" placeholder="25"
                  value={newItem.innerHeight} onChange={e => setNewItem(v => ({ ...v, innerHeight: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>截面積 mm²</label>
                <input className="lib-input" style={{ width: 90 }} type="number" step="0.1" min="0" placeholder="625"
                  value={newItem.crossSection} onChange={e => setNewItem(v => ({ ...v, crossSection: e.target.value }))} />
              </div>
              <div className="lib-form-field">
                <label>材質</label>
                <input className="lib-input" style={{ width: 100 }} placeholder="鋁合金"
                  value={newItem.material} onChange={e => setNewItem(v => ({ ...v, material: e.target.value }))} />
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

        {/* 搜尋欄 */}
        <div className="catalog-filter">
          <div className="lib-search-wrap">
            <svg className="lib-search-icon" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            <input className="search-input lib-search" placeholder="搜尋型號、材質…"
              value={search} onChange={e => setSearch(e.target.value)} />
          </div>
        </div>

        {/* 數據表格 */}
        <div className="tbl-scroll">
          <table className="data-tbl">
            <thead>
              <tr>
                <th style={{ minWidth: 40 }}>ID</th>
                <th>型號</th>
                <th style={{ minWidth: 70 }}>外尺寸 mm</th>
                <th style={{ minWidth: 70 }}>內尺寸 mm</th>
                <th style={{ minWidth: 70 }}>截面積 mm²</th>
                <th style={{ minWidth: 70 }}>材質</th>
                <th>備註</th>
                <th style={{ minWidth: 100 }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(r => (
                <tr key={r.id}>
                  <td style={{ color: 'var(--text3)', fontFamily: 'var(--font-mono)' }}>{r.id}</td>
                  <td><span className="lib-name-cell">{r.model}</span></td>
                  <td>{r.width}×{r.height}</td>
                  <td>{r.innerWidth}×{r.innerHeight}</td>
                  <td>{r.crossSection}</td>
                  <td>{r.material || '–'}</td>
                  <td style={{ color: 'var(--text3)' }}>{r.remarks || '–'}</td>
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
                          <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
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
                        <line x1="9" y1="21" x2="9" y2="9"/>
                      </svg>
                      <span>沒有找到匹配的記錄</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* 底部計數 */}
        <div className="record-count">
          <span>共 {filtered.length} 筆記錄</span>
        </div>
      </div>

      {/* 編輯模態框 */}
      {editTarget && (
        <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) closeEdit(); }}>
          <div className="modal-box lib-edit-modal">
            <div className="modal-head">
              <div className="lib-modal-title">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
                編輯線槽
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
                  <input className="lib-input" style={{ width: '100%' }} placeholder="型號"
                    value={editForm.model} onChange={e => setEditForm(v => ({ ...v, model: e.target.value }))} autoFocus />
                </div>
                <div className="lib-form-field">
                  <label>外寬 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.width} onChange={e => setEditForm(v => ({ ...v, width: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>外高 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.height} onChange={e => setEditForm(v => ({ ...v, height: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>內寬 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.innerWidth} onChange={e => setEditForm(v => ({ ...v, innerWidth: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>內高 mm <span className="required-mark">*</span></label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" min="0"
                    value={editForm.innerHeight} onChange={e => setEditForm(v => ({ ...v, innerHeight: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>截面積 mm²</label>
                  <input className="lib-input" style={{ width: '100%' }} type="number" step="0.1" min="0"
                    value={editForm.crossSection} onChange={e => setEditForm(v => ({ ...v, crossSection: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>材質</label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="鋁合金"
                    value={editForm.material} onChange={e => setEditForm(v => ({ ...v, material: e.target.value }))} />
                </div>
                <div className="lib-form-field">
                  <label>備註</label>
                  <input className="lib-input" style={{ width: '100%' }} placeholder="選填"
                    value={editForm.remarks} onChange={e => setEditForm(v => ({ ...v, remarks: e.target.value }))} />
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
        message={`確定要刪除型號「${data.find(r => r.id === confirmId)?.model ?? ''}」嗎？此操作不可撤銷。`}
        confirmLabel="刪除"
        confirmDanger
        onConfirm={() => {
          if (confirmId !== null) {
            trunkingApi.delete(confirmId).then(load).catch(console.error);
            setConfirmId(null);
          }
        }}
        onCancel={() => setConfirmId(null)}
      />
    </>
  );
};

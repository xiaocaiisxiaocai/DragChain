import React, { useState, useEffect } from 'react';
import { pipeLibraryApi } from '../api/pipeLibrary';
import { ConfirmModal } from './ConfirmModal';

interface PipeType {
  id: number;
  name: string;
  type: string;
  diameter: number;
  weight: number;
  bendMultiplier: number;
}

interface EditFormData {
  name: string;
  type: string;
  diameter: string;
  weight: string;
  bendMultiplier: string;
}

const PIPE_TYPE_OPTIONS = [
  { value: 'cable', label: '电缆' },
  { value: 'tube', label: '气管 / 水管' },
  { value: 'encoder', label: '编码器线' },
  { value: 'other', label: '其他' },
];

const TYPE_COLOR_MAP: Record<string, string> = {
  cable: 'pipe-badge-cable',
  tube: 'pipe-badge-tube',
  encoder: 'pipe-badge-encoder',
  other: 'pipe-badge-other',
};

const TYPE_LABEL_MAP: Record<string, string> = {
  cable: '电缆',
  tube: '气管/水管',
  encoder: '编码器线',
  other: '其他',
};

export const PipeLibraryPage: React.FC = () => {
  const [data, setData] = useState<PipeType[]>([]);
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [newItem, setNewItem] = useState<EditFormData>({
    name: '', type: 'cable', diameter: '', weight: '', bendMultiplier: '8',
  });
  const [addError, setAddError] = useState('');

  // 编辑模态框状态
  const [editTarget, setEditTarget] = useState<PipeType | null>(null);
  const [editForm, setEditForm] = useState<EditFormData>({
    name: '', type: 'cable', diameter: '', weight: '', bendMultiplier: '8',
  });
  const [editError, setEditError] = useState('');
  const [confirmId, setConfirmId] = useState<number | null>(null);

  const load = () => pipeLibraryApi.getAll().then(setData).catch(console.error);
  useEffect(() => { load(); }, []);

  const openEdit = (r: PipeType) => {
    setEditTarget(r);
    setEditForm({
      name: r.name,
      type: r.type,
      diameter: String(r.diameter),
      weight: String(r.weight),
      bendMultiplier: String(r.bendMultiplier),
    });
    setEditError('');
  };

  const closeEdit = () => {
    setEditTarget(null);
    setEditError('');
  };

  const handleEditSave = () => {
    if (!editTarget) return;
    if (!editForm.name.trim()) { setEditError('名称不能为空'); return; }
    const diameter = parseFloat(editForm.diameter);
    const weight = parseFloat(editForm.weight);
    const bendMultiplier = parseInt(editForm.bendMultiplier);
    if (isNaN(diameter) || diameter <= 0) { setEditError('外径必须为正数'); return; }
    if (isNaN(weight) || weight < 0) { setEditError('重量必须为非负数'); return; }
    if (isNaN(bendMultiplier) || bendMultiplier <= 0) { setEditError('弯曲系数必须为正整数'); return; }
    setEditError('');
    pipeLibraryApi.update(editTarget.id, {
      name: editForm.name.trim(),
      type: editForm.type,
      diameter,
      weight,
      bendMultiplier,
    }).then(() => {
      closeEdit();
      load();
    }).catch((e: Error) => setEditError(e.message || '保存失败'));
  };

  const handleDelete = (id: number) => {
    setConfirmId(id);
  };

  const handleAdd = () => {
    if (!newItem.name.trim()) { setAddError('名称不能为空'); return; }
    const diameter = parseFloat(newItem.diameter);
    const weight = parseFloat(newItem.weight);
    const bendMultiplier = parseInt(newItem.bendMultiplier);
    if (isNaN(diameter) || diameter <= 0) { setAddError('外径必须为正数'); return; }
    if (isNaN(weight) || weight < 0) { setAddError('重量必须为非负数'); return; }
    if (isNaN(bendMultiplier) || bendMultiplier <= 0) { setAddError('弯曲系数必须为正整数'); return; }
    setAddError('');
    pipeLibraryApi.create({
      name: newItem.name.trim(),
      type: newItem.type,
      diameter,
      weight,
      bendMultiplier,
    }).then(() => {
      setShowAddForm(false);
      setNewItem({ name: '', type: 'cable', diameter: '', weight: '', bendMultiplier: '8' });
      load();
    }).catch((e: Error) => setAddError(e.message || '新增失败'));
  };

  const filters = ['all', 'cable', 'tube', 'encoder', 'other'];
  const filtered = data.filter(r => {
    const matchFilter = filter === 'all' || r.type === filter;
    const matchSearch = !search ||
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.type.toLowerCase().includes(search.toLowerCase()) ||
      r.diameter.toString().includes(search) ||
      r.bendMultiplier.toString().includes(search);
    return matchFilter && matchSearch;
  });

  return (
    <>
      <div className="data-table-wrap" style={{ flex: 1, display: 'flex', flexDirection: 'column', margin: 'var(--space-2)', overflow: 'hidden' }}>
        {/* 顶部标题栏 */}
        <div className="lib-topbar">
          <div className="lib-topbar-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 2L2 7l10 5 10-5-10-5z"/>
              <path d="M2 17l10 5 10-5"/>
              <path d="M2 12l10 5 10-5"/>
            </svg>
            <span>管线库维护</span>
            <span className="lib-count-badge">{filtered.length}</span>
          </div>
          <button
            className="btn-primary-sm"
            onClick={() => setShowAddForm(v => !v)}
          >
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
                新增管线
              </>
            )}
          </button>
        </div>

        {/* 新增表单 */}
        {showAddForm && (
          <div className="lib-add-panel">
            <div className="lib-panel-header">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="16"/>
                <line x1="8" y1="12" x2="16" y2="12"/>
              </svg>
              新增管线类型
            </div>
            <div className="lib-add-grid">
              <div className="lib-form-field">
                <label>名称 <span className="required-mark">*</span></label>
                <input
                  className="lib-input"
                  style={{ width: 240 }}
                  placeholder="例如：传感器信号电缆 Φ6"
                  value={newItem.name}
                  onChange={e => setNewItem(v => ({ ...v, name: e.target.value }))}
                  autoFocus
                />
              </div>
              <div className="lib-form-field">
                <label>类型 <span className="required-mark">*</span></label>
                <select
                  className="lib-input"
                  style={{ width: 140 }}
                  value={newItem.type}
                  onChange={e => setNewItem(v => ({ ...v, type: e.target.value }))}
                >
                  {PIPE_TYPE_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
              <div className="lib-form-field">
                <label>外径 mm <span className="required-mark">*</span></label>
                <input
                  className="lib-input"
                  style={{ width: 100 }}
                  type="number"
                  step="0.1"
                  min="0"
                  placeholder="6.0"
                  value={newItem.diameter}
                  onChange={e => setNewItem(v => ({ ...v, diameter: e.target.value }))}
                />
              </div>
              <div className="lib-form-field">
                <label>重量 kg/m</label>
                <input
                  className="lib-input"
                  style={{ width: 100 }}
                  type="number"
                  step="0.0001"
                  min="0"
                  placeholder="0.0600"
                  value={newItem.weight}
                  onChange={e => setNewItem(v => ({ ...v, weight: e.target.value }))}
                />
              </div>
              <div className="lib-form-field">
                <label>弯曲系数</label>
                <input
                  className="lib-input"
                  style={{ width: 80 }}
                  type="number"
                  min="1"
                  placeholder="8"
                  value={newItem.bendMultiplier}
                  onChange={e => setNewItem(v => ({ ...v, bendMultiplier: e.target.value }))}
                />
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

        {/* 筛选栏 */}
        <div className="catalog-filter">
          {filters.map(f => (
            <button
              key={f}
              className={`filter-btn ${filter === f ? 'active' : ''}`}
              onClick={() => setFilter(f)}
            >
              {f === 'all' ? '全部' : TYPE_LABEL_MAP[f] || f}
            </button>
          ))}
          <div className="lib-search-wrap">
            <svg className="lib-search-icon" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            <input
              className="search-input lib-search"
              placeholder="搜索名称、类型、外径…"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
        </div>

        {/* 数据表格 */}
        <div className="tbl-scroll">
          <table className="data-tbl">
            <thead>
              <tr>
                <th style={{ minWidth: 40 }}>ID</th>
                <th>名称</th>
                <th style={{ minWidth: 80 }}>类型</th>
                <th style={{ minWidth: 70 }}>外径 mm</th>
                <th style={{ minWidth: 80 }}>重量 kg/m</th>
                <th style={{ minWidth: 70 }}>弯曲系数</th>
                <th style={{ minWidth: 100 }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(r => (
                <tr key={r.id}>
                  <td style={{ color: 'var(--text3)', fontFamily: 'var(--font-mono)' }}>{r.id}</td>
                  <td>
                    <span className="lib-name-cell">{r.name}</span>
                  </td>
                  <td>
                    <span className={`pipe-type-badge ${TYPE_COLOR_MAP[r.type] || TYPE_COLOR_MAP.other}`}>
                      {TYPE_LABEL_MAP[r.type] || r.type}
                    </span>
                  </td>
                  <td>{r.diameter}</td>
                  <td>{r.weight}</td>
                  <td>{r.bendMultiplier}</td>
                  <td>
                    <div className="lib-action-group">
                      <button className="lib-action-btn lib-action-edit" onClick={() => openEdit(r)}>
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                        </svg>
                        编辑
                      </button>
                      <button className="lib-action-btn lib-action-del" onClick={() => handleDelete(r.id)}>
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <polyline points="3 6 5 6 21 6"/>
                          <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                          <path d="M10 11v6"/><path d="M14 11v6"/>
                          <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                        </svg>
                        删除
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={7}>
                    <div className="lib-empty">
                      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/>
                        <polyline points="13 2 13 9 20 9"/>
                      </svg>
                      <span>没有找到匹配的记录</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* 底部计数 */}
        <div className="record-count">
          <span>共 {filtered.length} 條記錄</span>
        </div>
      </div>

      {/* 编辑模态框 */}
      {editTarget && (
        <div className="modal-overlay open" onClick={e => { if (e.target === e.currentTarget) closeEdit(); }}>
          <div className="modal-box lib-edit-modal">
            <div className="modal-head">
              <div className="lib-modal-title">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
                编辑管线
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
                  <label>名称 <span className="required-mark">*</span></label>
                  <input
                    className="lib-input"
                    style={{ width: '100%' }}
                    placeholder="管线名称"
                    value={editForm.name}
                    onChange={e => setEditForm(v => ({ ...v, name: e.target.value }))}
                    autoFocus
                  />
                </div>
                <div className="lib-form-field">
                  <label>类型 <span className="required-mark">*</span></label>
                  <select
                    className="lib-input"
                    style={{ width: '100%' }}
                    value={editForm.type}
                    onChange={e => setEditForm(v => ({ ...v, type: e.target.value }))}
                  >
                    {PIPE_TYPE_OPTIONS.map(o => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                </div>
                <div className="lib-form-field">
                  <label>外径 mm <span className="required-mark">*</span></label>
                  <input
                    className="lib-input"
                    style={{ width: '100%' }}
                    type="number"
                    step="0.1"
                    min="0"
                    placeholder="例如：6.0"
                    value={editForm.diameter}
                    onChange={e => setEditForm(v => ({ ...v, diameter: e.target.value }))}
                  />
                </div>
                <div className="lib-form-field">
                  <label>重量 kg/m</label>
                  <input
                    className="lib-input"
                    style={{ width: '100%' }}
                    type="number"
                    step="0.0001"
                    min="0"
                    placeholder="例如：0.0600"
                    value={editForm.weight}
                    onChange={e => setEditForm(v => ({ ...v, weight: e.target.value }))}
                  />
                </div>
                <div className="lib-form-field">
                  <label>弯曲系数</label>
                  <input
                    className="lib-input"
                    style={{ width: '100%' }}
                    type="number"
                    min="1"
                    placeholder="例如：8"
                    value={editForm.bendMultiplier}
                    onChange={e => setEditForm(v => ({ ...v, bendMultiplier: e.target.value }))}
                  />
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
        message={`確定要刪除管線「${data.find(r => r.id === confirmId)?.name ?? ''}」嗎？此操作不可撤銷。`}
        confirmLabel="刪除"
        confirmDanger
        onConfirm={() => {
          if (confirmId !== null) {
            pipeLibraryApi.delete(confirmId).then(load).catch(console.error);
            setConfirmId(null);
          }
        }}
        onCancel={() => setConfirmId(null)}
      />
    </>
  );
};

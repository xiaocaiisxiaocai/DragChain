import React from 'react';

type Section = 'chain' | 'trunking' | 'pipe';

interface Props {
  activeSection: Section;
  activeTab: string;
  onSectionChange: (section: Section) => void;
  onTabChange: (tab: string) => void;
}

const SECTIONS: { key: Section; label: string }[] = [
  { key: 'trunking', label: '線槽' },
  { key: 'chain',    label: '拖鏈' },
  { key: 'pipe',     label: '管線庫' },
];

const CHAIN_TABS: { key: string; label: string }[] = [
  { key: 'calc', label: '選型計算' },
  { key: 'wzl',  label: '沃德 WZL 型錄' },
  { key: 'me',   label: '犸幕 ME 型錄' },
];

const TRUNKING_TABS: { key: string; label: string }[] = [
  { key: 'trunking',          label: '線槽選型' },
  { key: 'trunking-catalog',  label: '線槽型錄' },
];

export const Header: React.FC<Props> = ({ activeSection, activeTab, onSectionChange, onTabChange }) => {
  const subTabs = activeSection === 'chain'
    ? CHAIN_TABS
    : activeSection === 'trunking'
    ? TRUNKING_TABS
    : null;

  return (
    <header className="app-header">
      <div className="logo">DRC</div>
      <div className="header-title">選型計算工具</div>
      <div className="header-sep" />

      {/* 主菜單按鈕 */}
      <nav className="section-nav">
        {SECTIONS.map(s => (
          <button
            key={s.key}
            className={`section-btn ${activeSection === s.key ? 'active' : ''}`}
            onClick={() => {
              onSectionChange(s.key);
            }}
          >
            {s.label}
          </button>
        ))}
      </nav>

      {/* 子 Tab（管線庫無子 Tab，不渲染） */}
      {subTabs && (
        <>
          <div className="header-sub-sep" />
          <nav className="tab-nav">
            {subTabs.map(t => (
              <button
                key={t.key}
                className={`tab-btn ${activeTab === t.key ? 'active' : ''}`}
                onClick={() => onTabChange(t.key)}
              >
                {t.label}
              </button>
            ))}
          </nav>
        </>
      )}
    </header>
  );
};

import React from 'react';
import type { MatchResult } from '../types';

interface Props {
  brand: string;
  results: MatchResult[];
}

const ok = (b: boolean) => b
  ? <span className="status-ok">OK</span>
  : <span className="status-ng">NG</span>;

export const MatchTable: React.FC<Props> = ({ brand, results }) => {
  return (
    <div className="data-table-wrap">
      <div className="data-table-head">
        <div className="data-table-title">型號匹配矩陣</div>
        <span className={`badge badge-${brand === 'wzl' ? 'blue' : 'green'}`}>{brand.toUpperCase()}</span>
      </div>
      <div className="tbl-scroll">
        <table className="data-tbl">
          <thead>
            <tr>
              <th>型號</th>
              <th>內高<br />A mm</th>
              <th>推薦R<br />mm</th>
              <th>內空<br />mm²</th>
              <th>內高</th>
              <th>彎曲R</th>
              <th>內空</th>
              <th>初步判定</th>
              <th>架空能力<br />mm</th>
              <th>架空</th>
              <th>最終判定</th>
            </tr>
          </thead>
          <tbody>
            {results.map((r, i) => (
              <tr key={i} className={r.okFinal ? 'highlight' : ''}>
                <td>{r.model}</td>
                <td>{r.innerHeight}</td>
                <td>{r.recRadius}</td>
                <td>{r.innerArea}</td>
                <td>{ok(r.okHeight)}</td>
                <td>{ok(r.okRadius)}</td>
                <td>{ok(r.okArea)}</td>
                <td>{ok(r.okPrelim)}</td>
                <td>{r.calcSpan > 0 ? Math.round(r.calcSpan) : '—'}</td>
                <td>{ok(r.okSpan)}</td>
                <td>{ok(r.okFinal)}</td>
              </tr>
            ))}
            {results.length === 0 && (
              <tr>
                <td colSpan={11} style={{ color: 'var(--text3)', textAlign: 'center', padding: '20px' }}>
                  請先填寫管線清單後計算
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

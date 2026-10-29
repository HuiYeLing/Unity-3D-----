import { useCallback, useEffect, useState } from 'react';
import { fetchIssues, updateIssueStatus } from './api';
import type { Issue, Status } from './types';
import './App.css';

const STATUS_LABEL: Record<Status, string> = {
  open: '待处理',
  in_progress: '处理中',
  resolved: '已解决',
};

const PRIORITY_LABEL: Record<string, string> = {
  low: '低',
  medium: '中',
  high: '高',
};

function fmtTime(iso: string): string {
  if (!iso) return '-';
  const d = new Date(iso);
  return isNaN(d.getTime()) ? iso : d.toLocaleString('zh-CN');
}

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setIssues(await fetchIssues());
    } catch (e) {
      // 后端不可用时不再抛出，页面保留错误横幅 + 重试按钮，绝不白屏
      setError(e instanceof Error ? e.message : '未知错误');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const changeStatus = async (issue: Issue, status: Status) => {
    if (status === issue.status) return;
    setUpdatingId(issue.id);
    setError(null);
    try {
      const updated = await updateIssueStatus(issue.id, status);
      setIssues((prev) => prev.map((it) => (it.id === updated.id ? updated : it)));
    } catch (e) {
      // 失败不静默：显示原因，本地状态保持原值（select 的 value 受控，自然回滚）
      setError(e instanceof Error ? e.message : '更新失败');
    } finally {
      setUpdatingId(null);
    }
  };

  const selected = issues.find((it) => it.id === selectedId) ?? null;

  return (
    <div className="page">
      <header className="header">
        <h1>巡检问题管理</h1>
        <button className="btn" onClick={() => void load()} disabled={loading}>
          刷新
        </button>
      </header>

      {error && (
        <div className="error-banner" role="alert">
          <span>⚠ {error}</span>
          <button className="btn btn-small" onClick={() => void load()}>
            重试
          </button>
        </div>
      )}

      <div className="layout">
        <main className="list-panel">
          {loading ? (
            <p className="empty">加载中…</p>
          ) : issues.length === 0 && !error ? (
            <p className="empty">暂无问题记录，请在 Unity 端提交</p>
          ) : (
            <table className="issue-table">
              <thead>
                <tr>
                  <th>标题</th>
                  <th>优先级</th>
                  <th>状态</th>
                  <th>上报时间</th>
                </tr>
              </thead>
              <tbody>
                {issues.map((it) => (
                  <tr
                    key={it.id}
                    className={it.id === selectedId ? 'selected' : ''}
                    onClick={() => setSelectedId(it.id)}
                  >
                    <td className="title-cell">{it.title}</td>
                    <td>
                      <span className={`badge priority-${it.priority}`}>
                        {PRIORITY_LABEL[it.priority] ?? it.priority}
                      </span>
                    </td>
                    <td onClick={(e) => e.stopPropagation()}>
                      <select
                        className={`status-select status-${it.status}`}
                        value={it.status}
                        disabled={updatingId === it.id}
                        onChange={(e) => void changeStatus(it, e.target.value as Status)}
                      >
                        <option value="open">待处理</option>
                        <option value="in_progress">处理中</option>
                        <option value="resolved">已解决</option>
                      </select>
                    </td>
                    <td className="time-cell">{fmtTime(it.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </main>

        <aside className="detail-panel">
          {!selected ? (
            <p className="empty">点击左侧问题查看详情</p>
          ) : (
            <>
              <h2>{selected.title}</h2>
              <dl className="detail">
                <dt>编号</dt>
                <dd>{selected.id}</dd>
                <dt>优先级</dt>
                <dd>
                  <span className={`badge priority-${selected.priority}`}>
                    {PRIORITY_LABEL[selected.priority] ?? selected.priority}
                  </span>
                </dd>
                <dt>状态</dt>
                <dd>{STATUS_LABEL[selected.status]}</dd>
                <dt>描述</dt>
                <dd>{selected.description || '（无描述）'}</dd>
                <dt>位置坐标</dt>
                <dd className="mono">
                  x={selected.position.x.toFixed(2)} y={selected.position.y.toFixed(2)}{' '}
                  z={selected.position.z.toFixed(2)}
                </dd>
                <dt>上报时间</dt>
                <dd>{fmtTime(selected.createdAt)}</dd>
                <dt>更新时间</dt>
                <dd>{fmtTime(selected.updatedAt)}</dd>
              </dl>
              <button className="btn" onClick={() => setSelectedId(null)}>
                关闭详情
              </button>
            </>
          )}
        </aside>
      </div>
    </div>
  );
}

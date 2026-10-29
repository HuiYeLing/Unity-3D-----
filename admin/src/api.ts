import type { Issue, IssueListResponse, Status } from './types';

const BASE_URL = 'http://127.0.0.1:8080';

/** 统一错误：网络不通 / 非JSON / 后端业务错误 都转成带中文 message 的异常 */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let resp: Response;
  try {
    resp = await fetch(BASE_URL + path, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    });
  } catch {
    // fetch 只在网络层失败时 reject：后端没启动、地址错误等
    throw new Error('无法连接后端服务，请确认 Go 服务已启动（127.0.0.1:8080）');
  }

  const text = await resp.text();
  let body: unknown = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    if (!resp.ok) throw new Error(`后端返回异常（HTTP ${resp.status}）`);
    throw new Error('后端返回的数据不是合法 JSON');
  }

  if (!resp.ok) {
    const err = body as { message?: string; error?: string } | null;
    throw new Error(err?.message || err?.error || `请求失败（HTTP ${resp.status}）`);
  }
  return body as T;
}

export function fetchIssues(): Promise<Issue[]> {
  return request<IssueListResponse>('/api/issues').then((r) => r.items ?? []);
}

export function updateIssueStatus(id: string, status: Status): Promise<Issue> {
  return request<Issue>(`/api/issues/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  });
}

// 三端字段完全一致的数据契约，与 Go models.go / Unity IssueModels.cs 对应
export const PRIORITIES = ['low', 'medium', 'high'] as const;
export const STATUSES = ['open', 'in_progress', 'resolved'] as const;

export type Priority = (typeof PRIORITIES)[number];
export type Status = (typeof STATUSES)[number];

export interface Position {
  x: number;
  y: number;
  z: number;
}

export interface Issue {
  id: string;
  title: string;
  description: string;
  priority: Priority;
  status: Status;
  position: Position;
  createdAt: string;
  updatedAt: string;
}

export interface IssueListResponse {
  items: Issue[];
}

export interface ApiError {
  error?: string;
  message?: string;
}

import type { AuthResponse, Group, Message, Paged, Post, User } from './types'

const baseUrl = import.meta.env.VITE_API_URL ?? '/api'

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) { super(message); this.status = status }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('university-social-token')
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers },
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}))
    throw new ApiError(response.status, problem.title ?? problem.detail ?? 'No se pudo completar la operación.')
  }
  return response.status === 204 ? undefined as T : response.json()
}

export const api = {
  register: (body: object) => request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  login: (body: object) => request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  dashboard: () => request<{ profile: User; postCount: number; groupCount: number; unreadMessages: number }>('/dashboard'),
  adminDashboard: () => request<Record<string, number>>('/dashboard/admin'),
  posts: (query = '') => request<Paged<Post>>(`/posts${query}`),
  createPost: (content: string, groupId?: string) => request<Post>('/posts', { method: 'POST', body: JSON.stringify({ content, groupId: groupId || null }) }),
  deletePost: (id: string) => request<void>(`/posts/${id}`, { method: 'DELETE' }),
  react: (id: string) => request<void>(`/posts/${id}/reaction`, { method: 'PUT', body: JSON.stringify({ type: 'like' }) }),
  comment: (postId: string, content: string) => request('/comments', { method: 'POST', body: JSON.stringify({ postId, content }) }),
  groups: (query = '') => request<Paged<Group>>(`/groups${query}`),
  createGroup: (name: string, description: string) => request<Group>('/groups', { method: 'POST', body: JSON.stringify({ name, description }) }),
  joinGroup: (id: string) => request<void>(`/groups/${id}/members`, { method: 'POST' }),
  users: (query = '') => request<Paged<User>>(`/users${query}`),
  updateUser: (id: string, body: object) => request<User>(`/users/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  adminUser: (id: string, body: object) => request<User>(`/users/${id}/administration`, { method: 'PATCH', body: JSON.stringify(body) }),
  messages: () => request<Message[]>('/messages'),
  conversation: (id: string) => request<Paged<Message>>(`/messages/conversation/${id}`),
  sendMessage: (recipientId: string, body: string) => request<Message>('/messages', { method: 'POST', body: JSON.stringify({ recipientId, body }) }),
  search: (q: string) => request<{ users: Paged<User>; groups: Paged<Group>; posts: Paged<Post> }>(`/search?q=${encodeURIComponent(q)}`),
}

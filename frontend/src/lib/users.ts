import { apiFetch } from './api'

export interface UserView {
  id: string
  email: string | null
  activeRole: string | null
  roles: string[]
}

export const usersApi = {
  list: () => apiFetch<UserView[]>('/api/users'),

  create: (body: { email: string; password: string; roles: string[]; activeRole: string }) =>
    apiFetch<{ id: string; email: string }>('/api/users', { method: 'POST', body: JSON.stringify(body) }),

  roles: (userId: string) => apiFetch<string[]>(`/api/users/${userId}/roles`),

  assignRole: (userId: string, role: string) =>
    apiFetch<void>(`/api/users/${userId}/roles`, { method: 'PUT', body: JSON.stringify({ role }) }),

  removeRole: (userId: string, role: string) =>
    apiFetch<void>(`/api/users/${userId}/roles/${encodeURIComponent(role)}`, { method: 'DELETE' }),

  setPassword: (userId: string, newPassword: string) =>
    apiFetch<void>(`/api/users/${userId}/password`, {
      method: 'PUT',
      body: JSON.stringify({ newPassword }),
    }),
}

export const ROLES_ALL = ['Superadmin', 'Operator', 'Supervisi', 'Tamu']

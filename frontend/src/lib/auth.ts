import {
  ACCESS_KEY,
  REFRESH_KEY,
  USER_KEY,
  ApiError,
  apiFetch,
  buildApiUrl,
  clearSession,
  getAccessToken,
} from './api'
import type { AuthUser } from './api'

interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresInMinutes: number
  email: string
  activeRole: string
  roles: string[]
}

function saveSession(response: LoginResponse): void {
  window.localStorage.setItem(ACCESS_KEY, response.accessToken)
  window.localStorage.setItem(REFRESH_KEY, response.refreshToken)
  const user: AuthUser = {
    email: response.email,
    activeRole: response.activeRole,
    roles: response.roles,
  }
  window.localStorage.setItem(USER_KEY, JSON.stringify(user))
}

export function getSession(): AuthUser | null {
  if (typeof window === 'undefined') {
    return null
  }
  const raw = window.localStorage.getItem(USER_KEY)
  if (!raw || !getAccessToken()) {
    return null
  }
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

export { clearSession }

export async function login(email: string, password: string): Promise<LoginResponse> {
  const res = await fetch(buildApiUrl('/api/auth/login'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  if (!res.ok) {
    let body: unknown = null
    try {
      body = await res.json()
    } catch {
      // bukan JSON
    }
    const detail = (body as { detail?: string } | null)?.detail
    throw new ApiError(res.status, detail ?? 'Login gagal.')
  }
  return (await res.json()) as LoginResponse
}

export async function loginAndPersist(email: string, password: string): Promise<AuthUser> {
  const response = await login(email, password)
  saveSession(response)
  const session = getSession()
  if (!session) {
    throw new Error('Sesi gagal dimuat setelah login.')
  }
  return session
}

export async function switchRole(role: string): Promise<void> {
  const response = await apiFetch<LoginResponse>('/api/auth/switch-role', {
    method: 'POST',
    body: JSON.stringify({ role }),
  })
  saveSession(response)
}

export async function refreshSession(): Promise<boolean> {
  const refreshToken =
    typeof window === 'undefined' ? null : window.localStorage.getItem(REFRESH_KEY)
  if (!refreshToken) {
    return false
  }

  const res = await fetch(buildApiUrl('/api/auth/refresh'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
  if (!res.ok) {
    return false
  }
  saveSession((await res.json()) as LoginResponse)
  return true
}

export async function logout(): Promise<void> {
  try {
    await apiFetch<void>('/api/auth/logout', { method: 'POST' })
  } catch {
    // sesi lokal tetap dibuang walau server gagal memproses
  }
  clearSession()
}

const API_BASE_URL: string = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? ''

export const ACCESS_KEY = 'sm_access'
export const REFRESH_KEY = 'sm_refresh'
export const USER_KEY = 'sm_user'

export interface AuthUser {
  email: string
  activeRole: string
  roles: string[]
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function buildApiUrl(path: string, baseUrl: string = API_BASE_URL): string {
  return `${baseUrl.replace(/\/$/, '')}${path}`
}

export function getAccessToken(): string | null {
  if (typeof window === 'undefined') {
    return null
  }
  return window.localStorage.getItem(ACCESS_KEY)
}

export function clearSession(): void {
  if (typeof window === 'undefined') {
    return
  }
  window.localStorage.removeItem(ACCESS_KEY)
  window.localStorage.removeItem(REFRESH_KEY)
  window.localStorage.removeItem(USER_KEY)
}

export function problemMessage(status: number, body: unknown): string {
  const detail = (body as { detail?: string } | null)?.detail
  return detail ?? `Permintaan gagal (HTTP ${status}).`
}

function authHeaders(): Record<string, string> {
  const token = getAccessToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function parseError(res: Response): Promise<ApiError> {
  let body: unknown = null
  try {
    body = await res.json()
  } catch {
    // body bukan JSON (mis. HTML error page)
  }
  return new ApiError(res.status, problemMessage(res.status, body))
}

async function rawFetch(path: string, init: RequestInit | undefined): Promise<Response> {
  return fetch(buildApiUrl(path), {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders(),
      ...(init?.headers ?? {}),
    },
  })
}

export type RefreshAction = () => Promise<boolean>

let refreshAction: RefreshAction | null = null

export function setRefreshAction(action: RefreshAction): void {
  refreshAction = action
}

/**
 * Fetch ke API dengan Bearer token. 401 → coba refresh sekali → ulangi permintaan;
 * bila refresh gagal: bersihkan sesi dan lempar ApiError(401) (pemanggil redirect /login).
 * 403 → ApiError(403) (notice "tanpa wewenang").
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let res = await rawFetch(path, init)

  if (res.status === 401 && getAccessToken() && refreshAction) {
    const refreshed = await refreshAction()
    if (refreshed) {
      res = await rawFetch(path, init)
    }
  }

  if (!res.ok) {
    if (res.status === 401) {
      clearSession()
    }
    throw await parseError(res)
  }

  if (res.status === 204) {
    return undefined as T
  }

  // Beberapa endpoint sukses mengembalikan body kosong (mis. Results.Ok() tanpa nilai) —
  // parsing "" akan melempar SyntaxError; perlakukan body kosong sebagai undefined.
  const text = await res.text()
  if (text === '') {
    return undefined as T
  }

  return JSON.parse(text) as T
}

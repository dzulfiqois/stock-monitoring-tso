import { useEffect, useState } from 'react'
import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import { ApiError } from '../lib/api'
import { getSession, loginAndPersist, switchRole } from '../lib/auth'
import { useIsClient } from '../lib/useIsClient'

export const Route = createFileRoute('/login')({
  beforeLoad: () => {
    if (typeof window !== 'undefined' && getSession()) {
      throw redirect({ to: '/' })
    }
  },
  component: LoginPage,
})

function LoginPage() {
  const navigate = useNavigate()
  const isClient = useIsClient()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [pending, setPending] = useState<{ roles: string[] } | null>(null)

  // Direct URL load: server tidak melihat localStorage — gate auth dijalankan di client.
  // beforeLoad tetap menangani SPA navigation.
  useEffect(() => {
    if (isClient && getSession()) {
      void navigate({ to: '/' })
    }
  }, [isClient, navigate])

  if (!isClient) {
    return (
      <main className="sm-auth-shell">
        <div className="sm-auth-card" />
      </main>
    )
  }

  async function finish(roles: string[]) {
    if (roles.length > 1) {
      setPending({ roles })
      return
    }
    await navigate({ to: '/' })
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const user = await loginAndPersist(email, password)
      await finish(user.roles)
    } catch (ex) {
      setError(ex instanceof ApiError ? ex.message : 'Error: Invalid login attempt.')
    } finally {
      setBusy(false)
    }
  }

  async function onPickRole(role: string) {
    setError(null)
    setBusy(true)
    try {
      await switchRole(role)
      await navigate({ to: '/' })
    } catch (ex) {
      setError(ex instanceof ApiError ? ex.message : 'Gagal memilih role.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="sm-auth-shell">
      <div className="sm-auth-card">
        <div className="sm-auth-brand">
          <img
            src="/images/Pertamina_Logo.svg"
            alt="Pertamina Logo"
            style={{ height: 28, width: 'auto', maxWidth: 160, objectFit: 'contain' }}
          />
          <span>Stock Monitor &amp; TSO</span>
        </div>

        {pending ? (
          <>
            <hr className="my-3 border-sm-outline-variant" />
            <p className="mb-3 text-sm text-sm-on-surface-variant">
              Pilih role aktif — hak akses mengikuti role aktif, bukan gabungan seluruh role.
            </p>
            <div className="flex flex-col gap-2">
              {pending.roles.map((role) => (
                <button
                  key={role}
                  type="button"
                  disabled={busy}
                  className="w-full rounded-md border border-[#ced4da] bg-white px-3 py-2 text-left text-sm font-semibold text-sm-primary hover:bg-sm-surface-low"
                  onClick={() => onPickRole(role)}
                >
                  {role}
                </button>
              ))}
            </div>
            {error ? <StatusMessage message={error} /> : null}
          </>
        ) : (
          <>
            <StatusMessage message={error} />
            <hr className="my-3 border-sm-outline-variant" />
            <form onSubmit={onSubmit} noValidate={false}>
              <div className="relative mb-3">
                <input
                  id="email"
                  type="email"
                  required
                  autoComplete="username"
                  aria-required="true"
                  placeholder="name@example.com"
                  className="peer h-14 w-full rounded-md border border-[#ced4da] bg-white px-3 pb-1.5 pt-5 text-sm text-sm-on-surface outline-none focus:border-sm-primary focus:ring-1 focus:ring-sm-primary"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
                <label
                  htmlFor="email"
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-sm-on-surface-variant transition-all peer-focus:top-2.5 peer-focus:text-xs peer-[:not(:placeholder-shown)]:top-2.5 peer-[:not(:placeholder-shown)]:text-xs"
                >
                  Email
                </label>
              </div>
              <div className="relative mb-3">
                <input
                  id="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  aria-required="true"
                  placeholder="password"
                  className="peer h-14 w-full rounded-md border border-[#ced4da] bg-white px-3 pb-1.5 pt-5 text-sm text-sm-on-surface outline-none focus:border-sm-primary focus:ring-1 focus:ring-sm-primary"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <label
                  htmlFor="password"
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-sm-on-surface-variant transition-all peer-focus:top-2.5 peer-focus:text-xs peer-[:not(:placeholder-shown)]:top-2.5 peer-[:not(:placeholder-shown)]:text-xs"
                >
                  Password
                </label>
              </div>
              <div className="mb-3">
                <label className="flex cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    className="h-4 w-4 accent-sm-primary"
                    checked={rememberMe}
                    onChange={(e) => setRememberMe(e.target.checked)}
                  />
                  Remember me
                </label>
              </div>
              <div>
                <button
                  type="submit"
                  disabled={busy}
                  className="w-full rounded-md bg-[#0d6efd] px-4 py-2.5 text-base font-normal text-white hover:bg-[#0b5ed7] disabled:opacity-60"
                >
                  {busy ? 'Memproses…' : 'Log in'}
                </button>
              </div>
            </form>
          </>
        )}
      </div>
    </main>
  )
}

function StatusMessage({ message }: { message: string | null }) {
  if (!message) {
    return null
  }
  return (
    <div
      role="alert"
      className="rounded-md border border-sm-error-container bg-sm-error-container px-3 py-2 text-sm text-sm-on-error-container"
    >
      {message}
    </div>
  )
}

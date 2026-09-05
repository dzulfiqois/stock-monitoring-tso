import { useEffect } from 'react'
import { Link, createFileRoute, Outlet, redirect, useNavigate } from '@tanstack/react-router'
import { getSession, logout, switchRole } from '../lib/auth'
import { useIsClient } from '../lib/useIsClient'

export const Route = createFileRoute('/_app')({
  beforeLoad: () => {
    if (typeof window !== 'undefined' && !getSession()) {
      throw redirect({ to: '/login' })
    }
  },
  component: AppShell,
})

function NavItem({
  to,
  exact,
  icon,
  label,
}: {
  to: string
  exact?: boolean
  icon: string
  label: string
}) {
  return (
    <Link
      to={to}
      className="sm-nav-item"
      activeOptions={{ exact: exact ?? false }}
      activeProps={{ className: 'sm-nav-item active' }}
    >
      <span className="material-symbols-outlined">{icon}</span> {label}
    </Link>
  )
}

function NavMenu({
  email,
  roles,
  activeRole,
  onRoleChange,
  onLogout,
}: {
  email: string
  roles: string[]
  activeRole: string
  onRoleChange: (role: string) => void
  onLogout: () => void
}) {
  const isSuperadmin = activeRole === 'Superadmin'
  return (
    <nav className="sm-nav">
      <NavItem to="/" exact icon="dashboard" label="Ringkasan Dashboard" />
      <NavItem to="/gudang-wilayah" icon="factory" label="Gudang Wilayah" />
      <NavItem to="/tso" icon="local_shipping" label="Transport Shipping Order" />
      {isSuperadmin ? (
        <NavItem to="/mitra" icon="handshake" label="Mitra TSO" />
      ) : null}
      <NavItem to="/sales-area/register" icon="add_business" label="Register Sales Area" />
      {isSuperadmin ? (
        <NavItem to="/admin/users" icon="manage_accounts" label="Manajemen User" />
      ) : null}

      <div style={{ flex: 1 }} />

      <div style={{ padding: '8px 16px' }}>
        <div className="sm-label-caps" style={{ marginBottom: 4 }}>
          Role aktif
        </div>
        <select
          className="sm-filter"
          style={{ width: '100%' }}
          value={activeRole}
          onChange={(e) => onRoleChange(e.target.value)}
        >
          {roles.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
      </div>
      <div className="sm-nav-item" style={{ cursor: 'default' }}>
        <span className="material-symbols-outlined">account_circle</span> {email}
      </div>
      <button type="button" className="sm-nav-item" onClick={onLogout}>
        <span className="material-symbols-outlined">logout</span> Logout
      </button>
    </nav>
  )
}

function AppShell() {
  const navigate = useNavigate()
  const isClient = useIsClient()
  const session = isClient ? getSession() : null

  useEffect(() => {
    if (isClient && !getSession()) {
      void navigate({ to: '/login' })
    }
  }, [isClient, navigate])

  async function onRoleChange(role: string) {
    await switchRole(role)
    // Mirror ActiveRoleSwitcher Blazor: force reload agar seluruh data dirender ulang
    // dengan role aktif baru.
    window.location.reload()
  }

  async function onLogout() {
    await logout()
    await navigate({ to: '/login' })
  }

  if (!isClient) {
    return <div className="sm-shell" />
  }

  if (!session) {
    return null
  }

  return (
    <div className="sm-shell">
      <aside className="sm-sidebar">
        <div className="sm-sidebar-brand">
          <img
            src="/images/Pertamina_Logo.svg"
            alt="Pertamina Logo"
            style={{ height: 22, width: 'auto', maxWidth: 140, objectFit: 'contain' }}
          />
          <span>Stock Monitor &amp; TSO</span>
        </div>
        <NavMenu
          email={session.email}
          roles={session.roles}
          activeRole={session.activeRole}
          onRoleChange={onRoleChange}
          onLogout={onLogout}
        />
      </aside>

      <div className="sm-main">
        <div className="sm-topbar">
          <span className="sm-body-md" style={{ color: 'var(--sm-on-surface-variant)' }}>
            Dashboard
          </span>
          <span className="sm-pill sm-pill-neutral" style={{ gap: 8 }}>
            <span
              style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--sm-primary)' }}
            />
            {session.email}
          </span>
        </div>
        <main className="sm-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

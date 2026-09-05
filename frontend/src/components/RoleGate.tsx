import type { ReactNode } from 'react'
import { getSession } from '../lib/auth'

/// Mirror <AuthorizeView Roles="..."> Blazor: children hanya render bila
/// role aktif termasuk daftar. Enforcement tetap di API — ini hanya menyembunyikan kontrol.
export function RoleGate({ roles, children }: { roles: string[]; children: ReactNode }) {
  const role = getSession()?.activeRole
  if (!role || !roles.includes(role)) {
    return null
  }
  return <>{children}</>
}

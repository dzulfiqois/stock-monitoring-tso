import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { usersApi } from '../lib/users'
import type { UserView } from '../lib/users'

export const Route = createFileRoute('/_app/admin/users')({
  component: UserManagementPage,
})

const EMPTY_CREATE = { email: '', password: '', confirm: '', activeRole: '', roles: [] as string[] }

function UserManagementPage() {
  const queryClient = useQueryClient()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [createForm, setCreateForm] = useState(EMPTY_CREATE)
  const [formMessage, setFormMessage] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [pageError, setPageError] = useState<string | null>(null)

  const users = useQuery({ queryKey: ['users'], queryFn: usersApi.list })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['users'] })

  const createMutation = useMutation({
    mutationFn: usersApi.create,
    onSuccess: () => {
      setShowCreateModal(false)
      setCreateForm(EMPTY_CREATE)
      void invalidate()
    },
    onError: (error: Error) => setFormMessage(error.message),
  })

  const assignMutation = useMutation({
    mutationFn: (args: { userId: string; role: string }) => usersApi.assignRole(args.userId, args.role),
    onSuccess: () => void invalidate(),
    onError: (error: Error) => setPageError(error.message),
  })
  const removeMutation = useMutation({
    mutationFn: (args: { userId: string; role: string }) => usersApi.removeRole(args.userId, args.role),
    onSuccess: () => void invalidate(),
    onError: (error: Error) => setPageError(error.message),
  })
  const passwordMutation = useMutation({
    mutationFn: (args: { userId: string; newPassword: string }) => usersApi.setPassword(args.userId, args.newPassword),
    onSuccess: () => setPageError(null),
    onError: (error: Error) => setPageError(error.message),
  })

  function toggleRole(user: UserView, role: string) {
    if (user.roles.includes(role)) {
      removeMutation.mutate({ userId: user.id, role })
    } else {
      assignMutation.mutate({ userId: user.id, role })
    }
  }

  function setPassword(user: UserView) {
    const newPassword = window.prompt(`Password baru untuk ${user.email ?? user.id}:`)
    if (newPassword === null) return
    if (newPassword.length < 8) {
      setPageError('Password minimal 8 karakter.')
      return
    }
    passwordMutation.mutate({ userId: user.id, newPassword })
  }

  function openCreateModal() {
    setCreateForm(EMPTY_CREATE)
    setFormMessage(null)
    setShowCreateModal(true)
  }

  function submitCreate() {
    if (createForm.password !== createForm.confirm) {
      setFormMessage('Konfirmasi password tidak sama.')
      return
    }
    if (createForm.roles.length === 0) {
      setFormMessage('Pilih minimal satu role.')
      return
    }
    if (!createForm.roles.includes(createForm.activeRole)) {
      setFormMessage('Role aktif harus salah satu dari role terpilih.')
      return
    }
    setIsCreating(true)
    createMutation.mutate(
      {
        email: createForm.email,
        password: createForm.password,
        roles: createForm.roles,
        activeRole: createForm.activeRole,
      },
      {
        onSettled: () => setIsCreating(false),
      },
    )
  }

  function toggleCreateRole(role: string) {
    setCreateForm((prev) => {
      const has = prev.roles.includes(role)
      const roles = has ? prev.roles.filter((r) => r !== role) : [...prev.roles, role]
      const activeRole =
        prev.activeRole && roles.includes(prev.activeRole) ? prev.activeRole : roles[0] ?? ''
      return { ...prev, roles, activeRole }
    })
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <h1 className="sm-display-lg">Manajemen User</h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span className="sm-label-caps">Total {users.data?.length ?? 0} user</span>
          <button type="button" className="sm-btn sm-btn-primary sm-btn-sm" onClick={openCreateModal}>
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>person_add</span> Tambah User
          </button>
        </div>
      </div>

      {pageError ? <div className="sm-alert sm-alert-error">{pageError}</div> : null}

      {users.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>Memuat...</div>
      ) : (
        <div className="sm-table-wrap">
          <div style={{ overflowX: 'auto' }}>
            <table className="sm-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Role Aktif</th>
                  <th>Roles (klik untuk toggle)</th>
                  <th>Aksi</th>
                </tr>
              </thead>
              <tbody>
                {(users.data ?? []).map((user) => (
                  <tr key={user.id}>
                    <td style={{ fontWeight: 600 }}>{user.email}</td>
                    <td>
                      <span className="sm-pill sm-pill-neutral">{user.activeRole ?? '-'}</span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        {['Superadmin', 'Operator', 'Supervisi', 'Tamu'].map((role) => {
                          const active = user.roles.includes(role)
                          return (
                            <button
                              key={role}
                              type="button"
                              className={`sm-pill ${active ? 'sm-pill-success' : 'sm-pill-neutral'}`}
                              style={{ border: 'none', cursor: 'pointer' }}
                              title={active ? `Klik untuk hapus role ${role}` : `Klik untuk tambah role ${role}`}
                              onClick={() => toggleRole(user, role)}
                            >
                              {role}
                            </button>
                          )
                        })}
                      </div>
                    </td>
                    <td>
                      <button type="button" className="sm-btn sm-btn-sm sm-btn-outline" onClick={() => setPassword(user)}>
                        Set Password
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {showCreateModal ? (
        <Modal title="Tambah User Baru" onClose={() => setShowCreateModal(false)}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Email</label>
              <input type="email" className="sm-input" value={createForm.email} onChange={(e) => setCreateForm((f) => ({ ...f, email: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Password</label>
              <input type="password" className="sm-input" value={createForm.password} onChange={(e) => setCreateForm((f) => ({ ...f, password: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Konfirmasi Password</label>
              <input type="password" className="sm-input" value={createForm.confirm} onChange={(e) => setCreateForm((f) => ({ ...f, confirm: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Role (pilih minimal satu)</label>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                {['Superadmin', 'Operator', 'Supervisi', 'Tamu'].map((role) => (
                  <label key={role} style={{ display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={createForm.roles.includes(role)}
                      onChange={() => toggleCreateRole(role)}
                    />{' '}
                    {role}
                  </label>
                ))}
              </div>
            </div>
            <div>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Role Aktif</label>
              <select
                className="sm-input"
                value={createForm.activeRole}
                onChange={(e) => setCreateForm((f) => ({ ...f, activeRole: e.target.value }))}
              >
                {createForm.roles.map((role) => (
                  <option key={role} value={role}>{role}</option>
                ))}
              </select>
            </div>
            {formMessage ? <div className="sm-alert sm-alert-error" style={{ marginBottom: 0 }}>{formMessage}</div> : null}
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 16 }}>
            <button type="button" className="sm-btn sm-btn-outline" onClick={() => setShowCreateModal(false)}>Batal</button>
            <button type="button" className="sm-btn sm-btn-primary" disabled={isCreating} onClick={submitCreate}>
              {isCreating ? 'Menyimpan…' : 'Buat User'}
            </button>
          </div>
        </Modal>
      ) : null}
    </div>
  )
}

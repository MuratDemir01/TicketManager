import { FormEvent, useEffect, useState } from 'react'
import { api, ApiError } from '../api'
import { roleLabel } from '../auth'
import type { UserItem, UserRole } from '../types'

export function UsersPage() {
  const [users, setUsers] = useState<UserItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({
    userName: '',
    email: '',
    password: '',
    role: 'Employee' as UserRole,
  })

  async function load() {
    setError(null)
    try {
      setUsers(await api.users())
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kullanıcılar alınamadı')
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await api.createUser(form)
      setMessage('Kullanıcı oluşturuldu')
      setForm({ userName: '', email: '', password: '', role: 'Employee' })
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Oluşturma başarısız')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <h1>Kullanıcılar</h1>
          <p className="muted">Admin paneli — listeleme ve yeni kullanıcı.</p>
        </div>
      </header>

      {error && <div className="alert">{error}</div>}
      {message && <div className="ok">{message}</div>}

      <div className="grid-2">
        <section className="panel">
          <h2>Mevcut kullanıcılar</h2>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Kullanıcı</th>
                  <th>E-posta</th>
                  <th>Rol</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <td>{u.userName}</td>
                    <td>{u.email}</td>
                    <td>{roleLabel(u.role)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="panel">
          <h2>Yeni kullanıcı</h2>
          <form className="stack" onSubmit={onSubmit}>
            <label>
              Kullanıcı adı
              <input
                value={form.userName}
                onChange={(e) => setForm({ ...form, userName: e.target.value })}
                required
                maxLength={100}
              />
            </label>
            <label>
              E-posta
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                required
                maxLength={256}
              />
            </label>
            <label>
              Şifre
              <input
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                required
                minLength={6}
                maxLength={100}
              />
            </label>
            <label>
              Rol
              <select
                value={form.role}
                onChange={(e) =>
                  setForm({ ...form, role: e.target.value as UserRole })
                }
              >
                <option value="Employee">Employee</option>
                <option value="Admin">Admin</option>
              </select>
            </label>
            <button className="btn primary" type="submit" disabled={busy}>
              {busy ? 'Kaydediliyor…' : 'Oluştur'}
            </button>
          </form>
        </section>
      </div>
    </div>
  )
}

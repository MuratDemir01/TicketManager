import { FormEvent, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api'
import {
  PRIORITY_OPTIONS,
  priorityLabel,
  type TicketPriority,
  type UserItem,
} from '../types'

export function CreateTicketPage() {
  const navigate = useNavigate()
  const [employees, setEmployees] = useState<UserItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({
    title: '',
    description: '',
    customerName: '',
    customerEmail: '',
    priority: 'Normal' as TicketPriority,
    assignedUserId: '',
  })

  useEffect(() => {
    void api
      .users()
      .then((list) => setEmployees(list.filter((u) => u.role === 'Employee')))
      .catch(() => setEmployees([]))
  }, [])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const ticket = await api.createTicket({
        ...form,
        assignedUserId: form.assignedUserId || null,
      })
      navigate(`/tickets/${ticket.id}`)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Oluşturma başarısız')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page narrow">
      <header className="page-header">
        <div>
          <Link className="back" to="/tickets">
            ← Talepler
          </Link>
          <h1>Yeni talep</h1>
          <p className="muted">Yalnız Admin oluşturabilir. Atama opsiyonel; atanırsa Assigned olur.</p>
        </div>
      </header>

      {error && <div className="alert">{error}</div>}

      <form className="panel stack" onSubmit={onSubmit}>
        <label>
          Başlık
          <input
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
            required
            maxLength={150}
          />
        </label>
        <label>
          Açıklama
          <textarea
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            required
            rows={5}
          />
        </label>
        <label>
          Müşteri adı
          <input
            value={form.customerName}
            onChange={(e) => setForm({ ...form, customerName: e.target.value })}
            required
            maxLength={150}
          />
        </label>
        <label>
          Müşteri e-posta
          <input
            type="email"
            value={form.customerEmail}
            onChange={(e) => setForm({ ...form, customerEmail: e.target.value })}
            required
            maxLength={256}
          />
        </label>
        <label>
          Öncelik
          <select
            value={form.priority}
            onChange={(e) =>
              setForm({ ...form, priority: e.target.value as TicketPriority })
            }
          >
            {PRIORITY_OPTIONS.map((p) => (
              <option key={p} value={p}>
                {priorityLabel(p)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Atanan çalışan (opsiyonel)
          <select
            value={form.assignedUserId}
            onChange={(e) => setForm({ ...form, assignedUserId: e.target.value })}
          >
            <option value="">Atama yok</option>
            {employees.map((u) => (
              <option key={u.id} value={u.id}>
                {u.userName} · {u.email}
              </option>
            ))}
          </select>
        </label>
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'Kaydediliyor…' : 'Oluştur'}
        </button>
      </form>
    </div>
  )
}

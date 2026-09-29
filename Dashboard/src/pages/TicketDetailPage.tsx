import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api'
import { useAuth } from '../auth'
import {
  ALLOWED_TRANSITIONS,
  PRIORITY_OPTIONS,
  formatDate,
  priorityLabel,
  statusLabel,
  toPriority,
  toStatus,
  type TicketDetail,
  type TicketPriority,
  type TicketStatus,
  type UserItem,
} from '../types'

export function TicketDetailPage() {
  const { id } = useParams()
  const ticketId = Number(id)
  const navigate = useNavigate()
  const { user, isAdmin } = useAuth()
  const [ticket, setTicket] = useState<TicketDetail | null>(null)
  const [users, setUsers] = useState<UserItem[]>([])
  const [employees, setEmployees] = useState<UserItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [noteText, setNoteText] = useState('')
  const [edit, setEdit] = useState({
    title: '',
    description: '',
    customerName: '',
    customerEmail: '',
  })

  const status = ticket ? toStatus(ticket.ticketStatus) : null
  const nextStatuses = status
    ? ALLOWED_TRANSITIONS[status].filter(
        (s) => s !== 'Assigned' || !!ticket?.assignedUserId,
      )
    : []
  const isClosed = status === 'Closed'
  const canActAsAssignee =
    isAdmin ||
    (!!user &&
      !!ticket?.assignedUserId &&
      ticket.assignedUserId === user.userId)

  function resolveUser(userId?: string | null) {
    if (!userId) return '—'
    if (user && user.userId === userId) return `${user.userName} (sen)`
    const found = users.find((u) => u.id === userId)
    return found ? `${found.userName} (${found.email})` : userId
  }

  function formatHistoryValue(fieldName: string, value?: string | null) {
    if (value == null || value === '') return '—'
    if (fieldName === 'AssignedUserId') return resolveUser(value)
    if (fieldName === 'Status' || fieldName === 'TicketStatus')
      return statusLabel(value as TicketStatus) || value
    if (fieldName === 'Priority') return priorityLabel(value as TicketPriority) || value
    return value
  }

  async function load() {
    setError(null)
    try {
      const detail = await api.ticket(ticketId)
      setTicket(detail)
      setEdit({
        title: detail.title,
        description: detail.description,
        customerName: detail.customerName,
        customerEmail: detail.customerEmail,
      })
      if (isAdmin) {
        const list = await api.users()
        setUsers(list)
        setEmployees(list.filter((u) => u.role === 'Employee'))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Talep yüklenemedi')
      setTicket(null)
    }
  }

  useEffect(() => {
    if (!Number.isFinite(ticketId)) return
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ticketId, isAdmin])

  const assigneeName = useMemo(() => {
    if (!ticket?.assignedUserId) return 'Atanmamış'
    return resolveUser(ticket.assignedUserId)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ticket, users, user])

  async function run(action: () => Promise<unknown>, ok: string) {
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await action()
      setMessage(ok)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İşlem başarısız')
    } finally {
      setBusy(false)
    }
  }

  if (!Number.isFinite(ticketId)) {
    return <div className="alert">Geçersiz talep</div>
  }

  if (!ticket && !error) return <p className="muted">Yükleniyor…</p>
  if (!ticket) return <div className="alert">{error}</div>

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <Link className="back" to="/tickets">
            ← Talepler
          </Link>
          <h1>
            <span className="mono">{ticket.ticketNumber}</span> {ticket.title}
          </h1>
          <p className="muted">
            {statusLabel(ticket.ticketStatus)} · {priorityLabel(ticket.priority)} ·{' '}
            {assigneeName}
          </p>
        </div>
        {isAdmin && !isClosed && (
          <button
            className="btn danger"
            type="button"
            disabled={busy}
            onClick={() => {
              if (!confirm('Talep silinsin mi?')) return
              void run(async () => {
                await api.deleteTicket(ticket.id)
                navigate('/tickets')
              }, 'Silindi')
            }}
          >
            Sil
          </button>
        )}
      </header>

      {error && <div className="alert">{error}</div>}
      {message && <div className="ok">{message}</div>}

      <section className="panel meta-grid">
        <div>
          <span className="meta-label">Oluşturan</span>
          <strong>{resolveUser(ticket.createdByUserId)}</strong>
        </div>
        <div>
          <span className="meta-label">Atanan</span>
          <strong>{assigneeName}</strong>
        </div>
        <div>
          <span className="meta-label">Oluşturulma</span>
          <strong>{formatDate(ticket.createdAt)}</strong>
        </div>
        <div>
          <span className="meta-label">Son güncelleme</span>
          <strong>{formatDate(ticket.updatedAt)}</strong>
        </div>
        <div>
          <span className="meta-label">Durum</span>
          <strong>
            <span className={`pill status-${toStatus(ticket.ticketStatus)}`}>
              {statusLabel(ticket.ticketStatus)}
            </span>
          </strong>
        </div>
        <div>
          <span className="meta-label">Öncelik</span>
          <strong>
            <span className={`pill priority-${toPriority(ticket.priority)}`}>
              {priorityLabel(ticket.priority)}
            </span>
          </strong>
        </div>
      </section>

      <div className="grid-2">
        <section className="panel">
          <h2>Detay</h2>
          {isAdmin && !isClosed ? (
            <form
              className="stack"
              onSubmit={(e: FormEvent) => {
                e.preventDefault()
                void run(
                  () => api.updateTicket(ticket.id, edit),
                  'Talep güncellendi',
                )
              }}
            >
              <label>
                Başlık
                <input
                  value={edit.title}
                  onChange={(e) => setEdit({ ...edit, title: e.target.value })}
                  required
                  maxLength={150}
                />
              </label>
              <label>
                Açıklama
                <textarea
                  value={edit.description}
                  onChange={(e) =>
                    setEdit({ ...edit, description: e.target.value })
                  }
                  required
                  rows={5}
                />
              </label>
              <label>
                Müşteri
                <input
                  value={edit.customerName}
                  onChange={(e) =>
                    setEdit({ ...edit, customerName: e.target.value })
                  }
                  required
                  maxLength={150}
                />
              </label>
              <label>
                E-posta
                <input
                  type="email"
                  value={edit.customerEmail}
                  onChange={(e) =>
                    setEdit({ ...edit, customerEmail: e.target.value })
                  }
                  required
                  maxLength={256}
                />
              </label>
              <button className="btn primary" type="submit" disabled={busy}>
                Kaydet
              </button>
            </form>
          ) : (
            <div className="stack readonly">
              <p>{ticket.description}</p>
              <p>
                <strong>Müşteri:</strong> {ticket.customerName} ·{' '}
                {ticket.customerEmail}
              </p>
            </div>
          )}
        </section>

        <section className="panel">
          <h2>İşlemler</h2>
          <div className="stack">
            <div>
              <strong>Durum geçişi</strong>
              {isClosed ? (
                <p className="muted">Kapatılmış talep değiştirilemez.</p>
              ) : !canActAsAssignee ? (
                <p className="muted">
                  Bu talep sana atanmadığı için işlem yapamazsın.
                </p>
              ) : nextStatuses.length === 0 ? (
                <p className="muted">İleri durum yok.</p>
              ) : (
                <div className="btn-row">
                  {nextStatuses.map((s) => (
                    <button
                      key={s}
                      className="btn"
                      type="button"
                      disabled={busy}
                      onClick={() =>
                        void run(
                          () => api.changeStatus(ticket.id, s as TicketStatus),
                          `Durum: ${statusLabel(s)}`,
                        )
                      }
                    >
                      → {statusLabel(s)}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {isAdmin && !isClosed && (
              <>
                <label>
                  Öncelik
                  <select
                    value={toPriority(ticket.priority)}
                    disabled={busy}
                    onChange={(e) =>
                      void run(
                        () =>
                          api.changePriority(
                            ticket.id,
                            e.target.value as TicketPriority,
                          ),
                        'Öncelik güncellendi',
                      )
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
                  Atama (yalnız Employee)
                  <select
                    value={ticket.assignedUserId ?? ''}
                    disabled={busy}
                    onChange={(e) =>
                      void run(
                        () =>
                          api.assign(
                            ticket.id,
                            e.target.value ? e.target.value : null,
                          ),
                        'Atama güncellendi',
                      )
                    }
                  >
                    <option value="">Atanmamış</option>
                    {employees.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.userName} · {u.email}
                      </option>
                    ))}
                  </select>
                </label>
              </>
            )}

            {!isAdmin && (
              <p className="muted">
                Çalışan: öncelik / atama / silme / düzenleme yok. Not ve durum
                yalnız kendi talebinde.
              </p>
            )}
          </div>
        </section>
      </div>

      <section className="panel">
        <h2>Notlar</h2>
        {canActAsAssignee && !isClosed ? (
          <form
            className="note-form"
            onSubmit={(e) => {
              e.preventDefault()
              void run(async () => {
                await api.addNote(ticket.id, noteText.trim())
                setNoteText('')
              }, 'Not eklendi')
            }}
          >
            <textarea
              value={noteText}
              onChange={(e) => setNoteText(e.target.value)}
              placeholder="Not ekle…"
              maxLength={2000}
              required
              rows={3}
            />
            <button className="btn primary" type="submit" disabled={busy}>
              Not ekle
            </button>
          </form>
        ) : (
          <p className="muted">
            {isClosed
              ? 'Kapalı talebe not eklenemez.'
              : 'Not eklemek için talep sana atanmış olmalı (veya Admin ol).'}
          </p>
        )}

        <ul className="timeline">
          {ticket.notes.length === 0 && <li className="muted">Not yok.</li>}
          {ticket.notes.map((n) => (
            <li key={n.id}>
              <div className="timeline-meta">
                <span>{resolveUser(n.createdByUserId)}</span>
                <span>{formatDate(n.createdAt)}</span>
              </div>
              <p>{n.noteText}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="panel">
        <h2>Geçmiş</h2>
        <ul className="timeline">
          {ticket.histories.length === 0 && (
            <li className="muted">Kayıt yok.</li>
          )}
          {ticket.histories.map((h) => (
            <li key={h.id}>
              <div className="timeline-meta">
                <strong>{h.action}</strong>
                <span>{h.fieldName}</span>
                <span>{resolveUser(h.changedByUserId)}</span>
                <span>{formatDate(h.changedAt)}</span>
              </div>
              <p>
                {formatHistoryValue(h.fieldName, h.oldValue)} →{' '}
                {formatHistoryValue(h.fieldName, h.newValue)}
              </p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}

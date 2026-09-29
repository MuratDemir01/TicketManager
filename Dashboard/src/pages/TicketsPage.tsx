import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api'
import { useAuth } from '../auth'
import { ImportTicketsModal } from '../components/ImportTicketsModal'
import {
  PRIORITY_OPTIONS,
  STATUS_OPTIONS,
  formatDate,
  priorityLabel,
  statusLabel,
  toPriority,
  toStatus,
  type SortDirection,
  type Ticket,
  type TicketPriority,
  type TicketSortBy,
  type TicketStatus,
  type TicketSummary,
  type UserItem,
} from '../types'

export function TicketsPage() {
  const { isAdmin } = useAuth()
  const [items, setItems] = useState<Ticket[]>([])
  const [employees, setEmployees] = useState<UserItem[]>([])
  const [summary, setSummary] = useState<TicketSummary | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<TicketStatus | ''>('')
  const [priority, setPriority] = useState<TicketPriority | ''>('')
  const [assignedUserId, setAssignedUserId] = useState('')
  const [sort, setSort] = useState<TicketSortBy>('CreatedAt')
  const [direction, setDirection] = useState<SortDirection>('Desc')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [importOpen, setImportOpen] = useState(false)

  async function load(
    nextPage = page,
    nextSort = sort,
    nextDirection = direction,
  ) {
    setLoading(true)
    setError(null)
    try {
      const [list, sum] = await Promise.all([
        api.tickets({
          search: search.trim() || undefined,
          status,
          priority,
          assignedUserId: isAdmin ? assignedUserId || undefined : undefined,
          page: nextPage,
          pageSize: 20,
          sort: nextSort,
          direction: nextDirection,
        }),
        api.summary(),
      ])
      setItems(list.items)
      setTotalCount(list.totalCount)
      setPage(list.page)
      setSummary(sum)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Liste alınamadı')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load(1)
    if (isAdmin) {
      void api
        .users()
        .then((list) => setEmployees(list.filter((u) => u.role === 'Employee')))
        .catch(() => setEmployees([]))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin])

  function employeeLabel(userId?: string | null) {
    if (!userId) return '—'
    const found = employees.find((u) => u.id === userId)
    return found ? found.userName : userId
  }

  function onSortColumn(field: TicketSortBy) {
    const nextDirection: SortDirection =
      sort === field && direction === 'Desc' ? 'Asc' : 'Desc'
    setSort(field)
    setDirection(nextDirection)
    void load(1, field, nextDirection)
  }

  function sortArrow(field: TicketSortBy) {
    if (sort !== field) return null
    return (
      <span className="sort-arrow" aria-hidden="true">
        {direction === 'Desc' ? '↓' : '↑'}
      </span>
    )
  }

  async function exportCsv() {
    setExporting(true)
    setError(null)
    try {
      await api.exportCsv({
        search: search.trim() || undefined,
        status,
        priority,
        assignedUserId: isAdmin ? assignedUserId || undefined : undefined,
        sort,
        direction,
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Dışa aktarma başarısız')
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <h1>Talepler</h1>
          <p className="muted">
            {isAdmin
              ? 'Tüm talepleri yönetebilirsin.'
              : 'Yalnızca sana atanan talepler listelenir.'}
          </p>
        </div>
        <div className="header-actions">
          <button
            className="btn btn-icon"
            type="button"
            disabled={exporting || loading}
            onClick={() => void exportCsv()}
            title="CSV indir"
          >
            <svg
              className="icon"
              viewBox="0 0 24 24"
              width="18"
              height="18"
              aria-hidden="true"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M12 3v12" />
              <path d="m7 10 5 5 5-5" />
              <path d="M5 21h14" />
            </svg>
            {exporting ? 'Aktarılıyor…' : 'CSV indir'}
          </button>
          {isAdmin && (
            <button
              className="btn btn-icon"
              type="button"
              disabled={loading}
              onClick={() => setImportOpen(true)}
              title="CSV / Excel içe aktar"
            >
              <svg
                className="icon"
                viewBox="0 0 24 24"
                width="18"
                height="18"
                aria-hidden="true"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M12 21V9" />
                <path d="m7 14 5-5 5 5" />
                <path d="M5 3h14" />
              </svg>
              İçe aktar
            </button>
          )}
          {isAdmin && (
            <Link className="btn primary" to="/tickets/new">
              Yeni talep
            </Link>
          )}
        </div>
      </header>

      {isAdmin && (
        <ImportTicketsModal
          open={importOpen}
          onClose={() => setImportOpen(false)}
          onImported={() => void load(1)}
        />
      )}

      {summary && (
        <div className="stats">
          <div>
            <span>Toplam</span>
            <strong>{summary.total}</strong>
          </div>
          <div>
            <span>Açık</span>
            <strong>{summary.open}</strong>
          </div>
          <div>
            <span>Kritik</span>
            <strong>{summary.critical}</strong>
          </div>
          <div>
            <span>Çözüldü</span>
            <strong>{summary.resolved}</strong>
          </div>
          <div>
            <span>Kapalı</span>
            <strong>{summary.closed}</strong>
          </div>
        </div>
      )}

      <form
        className={`filters${isAdmin ? ' filters-admin' : ''}`}
        onSubmit={(e) => {
          e.preventDefault()
          void load(1)
        }}
      >
        <input
          placeholder="Talep no veya başlık ara…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          value={status}
          onChange={(e) => setStatus(e.target.value as TicketStatus | '')}
        >
          <option value="">Tüm durumlar</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {statusLabel(s)}
            </option>
          ))}
        </select>
        <select
          value={priority}
          onChange={(e) => setPriority(e.target.value as TicketPriority | '')}
        >
          <option value="">Tüm öncelikler</option>
          {PRIORITY_OPTIONS.map((p) => (
            <option key={p} value={p}>
              {priorityLabel(p)}
            </option>
          ))}
        </select>
        {isAdmin && (
          <select
            value={assignedUserId}
            onChange={(e) => setAssignedUserId(e.target.value)}
          >
            <option value="">Tümü</option>
            <option value="__unassigned__">Atanmamış</option>
            {employees.map((u) => (
              <option key={u.id} value={u.id}>
                {u.userName}
              </option>
            ))}
          </select>
        )}
        <button className="btn" type="submit">
          Filtrele
        </button>
      </form>

      {error && <div className="alert">{error}</div>}
      {loading ? (
        <p className="muted">Yükleniyor…</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'TicketNumber' ? ' active' : ''}`}
                    onClick={() => onSortColumn('TicketNumber')}
                  >
                    No{sortArrow('TicketNumber')}
                  </button>
                </th>
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'Title' ? ' active' : ''}`}
                    onClick={() => onSortColumn('Title')}
                  >
                    Başlık{sortArrow('Title')}
                  </button>
                </th>
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'CustomerName' ? ' active' : ''}`}
                    onClick={() => onSortColumn('CustomerName')}
                  >
                    Müşteri{sortArrow('CustomerName')}
                  </button>
                </th>
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'TicketStatus' ? ' active' : ''}`}
                    onClick={() => onSortColumn('TicketStatus')}
                  >
                    Durum{sortArrow('TicketStatus')}
                  </button>
                </th>
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'Priority' ? ' active' : ''}`}
                    onClick={() => onSortColumn('Priority')}
                  >
                    Öncelik{sortArrow('Priority')}
                  </button>
                </th>
                {isAdmin && (
                  <th>
                    <button
                      type="button"
                      className={`th-sort${sort === 'AssignedUserId' ? ' active' : ''}`}
                      onClick={() => onSortColumn('AssignedUserId')}
                    >
                      Atanan{sortArrow('AssignedUserId')}
                    </button>
                  </th>
                )}
                <th>
                  <button
                    type="button"
                    className={`th-sort${sort === 'CreatedAt' ? ' active' : ''}`}
                    onClick={() => onSortColumn('CreatedAt')}
                  >
                    Oluşturulma{sortArrow('CreatedAt')}
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={isAdmin ? 7 : 6} className="muted">
                    Kayıt yok.
                  </td>
                </tr>
              ) : (
                items.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <Link to={`/tickets/${t.id}`}>{t.ticketNumber}</Link>
                    </td>
                    <td>
                      <Link to={`/tickets/${t.id}`}>{t.title}</Link>
                    </td>
                    <td>{t.customerName}</td>
                    <td>
                      <span className={`pill status-${toStatus(t.ticketStatus)}`}>
                        {statusLabel(t.ticketStatus)}
                      </span>
                    </td>
                    <td>
                      <span className={`pill priority-${toPriority(t.priority)}`}>
                        {priorityLabel(t.priority)}
                      </span>
                    </td>
                    {isAdmin && <td>{employeeLabel(t.assignedUserId)}</td>}
                    <td>{formatDate(t.createdAt)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      <div className="pager">
        <button
          className="btn ghost"
          type="button"
          disabled={page <= 1 || loading}
          onClick={() => void load(page - 1)}
        >
          Önceki
        </button>
        <span>
          Sayfa {page} · {totalCount} kayıt
        </span>
        <button
          className="btn ghost"
          type="button"
          disabled={page * 20 >= totalCount || loading}
          onClick={() => void load(page + 1)}
        >
          Sonraki
        </button>
      </div>
    </div>
  )
}

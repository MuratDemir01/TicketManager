import { Navigate, Outlet, Link, useLocation } from 'react-router-dom'
import { roleLabel, useAuth } from '../auth'

export function ProtectedRoute({ adminOnly = false }: { adminOnly?: boolean }) {
  const { user, isAdmin } = useAuth()
  const location = useLocation()

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (adminOnly && !isAdmin) {
    return <Navigate to="/tickets" replace />
  }

  return <Outlet />
}

export function AppLayout() {
  const { user, isAdmin, logout } = useAuth()
  const location = useLocation()

  if (!user) return <Outlet />

  const ticketsActive =
    location.pathname === '/tickets' ||
    /^\/tickets\/\d+/.test(location.pathname)

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">TM</span>
          <div>
            <strong>TicketManager</strong>
            <small>Dashboard</small>
          </div>
        </div>

        <nav className="nav">
          <Link className={ticketsActive ? 'active' : ''} to="/tickets">
            Talepler
          </Link>
          {isAdmin && (
            <Link
              className={location.pathname.startsWith('/users') ? 'active' : ''}
              to="/users"
            >
              Kullanıcılar
            </Link>
          )}
        </nav>
      </aside>

      <div className="main-column">
        <header className="topbar">
          <div className="topbar-user">
            <strong>{user.userName}</strong>
            <span>
              {roleLabel(user.role)} · {user.email}
            </span>
          </div>
          <button type="button" className="btn topbar-logout" onClick={logout}>
            Çıkış
          </button>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

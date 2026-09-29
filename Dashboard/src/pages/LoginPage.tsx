import { FormEvent, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (user) return <Navigate to="/tickets" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(userNameOrEmail.trim(), password)
      const from = (location.state as { from?: string } | null)?.from || '/tickets'
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş başarısız')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={onSubmit}>
        <p className="eyebrow">TicketManager</p>
        <h1>Giriş yap</h1>
        <p className="muted">
          Admin tüm talepleri görür, çalışan yalnızca kendisine atananları.
        </p>

        <label>
          Kullanıcı adı veya e-posta
          <input
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
            autoComplete="username"
            required
          />
        </label>

        <label>
          Şifre
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
            minLength={6}
          />
        </label>

        {error && <div className="alert">{error}</div>}

        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'Giriş yapılıyor…' : 'Giriş'}
        </button>

        <div className="hint-box">
          <strong>Örnek hesaplar</strong>
          <span>Admin: ahmet.yilmaz@firma.com / Admin123!</span>
          <span>Employee: mehmet.demir@firma.com / Emp123!</span>
        </div>
      </form>
    </div>
  )
}

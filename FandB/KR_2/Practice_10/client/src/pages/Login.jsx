import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../state/AuthContext'

export default function LoginPage() {
  const { login } = useAuth()
  const nav = useNavigate()
  const loc = useLocation()
  const from = loc.state?.from || '/products'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await login({ email, password })
      nav(from, { replace: true })
    } catch (e2) {
      setError(e2?.response?.data?.error || 'Не удалось войти')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card row">
      <div>
        <h2>Вход</h2>
        <div className="muted">Нужен email и пароль</div>
      </div>
      <form className="row" onSubmit={onSubmit}>
        <div className="field">
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="user@example.com" />
        </div>
        <div className="field">
          <label>Пароль</label>
          <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
        </div>
        {error && <div className="error">{error}</div>}
        <div className="actions">
          <button className="btn primary" disabled={busy}>
            {busy ? 'Входим...' : 'Войти'}
          </button>
          <Link className="btn" to="/register">
            Регистрация
          </Link>
        </div>
      </form>
    </div>
  )
}


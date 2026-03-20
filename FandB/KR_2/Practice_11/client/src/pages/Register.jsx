import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../state/AuthContext'

export default function RegisterPage() {
  const { register } = useAuth()
  const nav = useNavigate()

  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await register({
        email,
        first_name: firstName,
        last_name: lastName,
        password,
      })
      nav('/login', { replace: true })
    } catch (e2) {
      setError(e2?.response?.data?.error || 'Не удалось зарегистрироваться')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card row">
      <div>
        <h2>Регистрация</h2>
        <div className="muted">Создание пользователя</div>
      </div>
      <form className="row" onSubmit={onSubmit}>
        <div className="field">
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="user@example.com" />
        </div>
        <div className="row2">
          <div className="field">
            <label>Имя</label>
            <input value={firstName} onChange={(e) => setFirstName(e.target.value)} />
          </div>
          <div className="field">
            <label>Фамилия</label>
            <input value={lastName} onChange={(e) => setLastName(e.target.value)} />
          </div>
        </div>
        <div className="field">
          <label>Пароль</label>
          <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
        </div>
        {error && <div className="error">{error}</div>}
        <div className="actions">
          <button className="btn primary" disabled={busy}>
            {busy ? 'Создаём...' : 'Зарегистрироваться'}
          </button>
          <Link className="btn" to="/login">
            Вход
          </Link>
        </div>
      </form>
    </div>
  )
}


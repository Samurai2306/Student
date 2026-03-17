import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import * as api from '../api/endpoints'
import { useAuth } from '../state/AuthContext'

const ROLES = ['user', 'seller', 'admin']

export default function UsersPage() {
  const { user } = useAuth()
  const nav = useNavigate()

  const isAdmin = useMemo(() => user?.role === 'admin', [user])
  const [items, setItems] = useState([])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(true)
  const [savingId, setSavingId] = useState('')

  useEffect(() => {
    if (!user) {
      nav('/login', { state: { from: '/users' } })
      return
    }
    if (!isAdmin) {
      nav('/products', { replace: true })
      return
    }
    ;(async () => {
      setBusy(true)
      setError('')
      try {
        const list = await api.listUsers()
        setItems(list || [])
      } catch (e) {
        setError(e?.response?.data?.error || 'Не удалось загрузить пользователей')
      } finally {
        setBusy(false)
      }
    })()
  }, [isAdmin, nav, user])

  async function updateRow(id, patch) {
    setSavingId(id)
    setError('')
    try {
      const updated = await api.updateUser(id, patch)
      setItems((prev) => prev.map((x) => (x.id === id ? updated : x)))
    } catch (e) {
      setError(e?.response?.data?.error || 'Не удалось обновить пользователя')
    } finally {
      setSavingId('')
    }
  }

  async function blockRow(id) {
    if (!confirm('Заблокировать пользователя?')) return
    setSavingId(id)
    setError('')
    try {
      const updated = await api.blockUser(id)
      setItems((prev) => prev.map((x) => (x.id === id ? updated : x)))
    } catch (e) {
      setError(e?.response?.data?.error || 'Не удалось заблокировать пользователя')
    } finally {
      setSavingId('')
    }
  }

  if (!isAdmin) return null

  return (
    <div className="card row">
      <div>
        <h2>Пользователи</h2>
        <div className="muted">Доступно только администратору</div>
      </div>

      {busy && <div className="muted">Загрузка...</div>}
      {error && <div className="error">{error}</div>}

      {!busy && (
        <div className="row" style={{ gap: 12 }}>
          {items.map((u) => (
            <div className="card row" key={u.id} style={{ gap: 10 }}>
              <div className="actions" style={{ justifyContent: 'space-between' }}>
                <div className="row" style={{ gap: 4 }}>
                  <div style={{ fontWeight: 700 }}>{u.email}</div>
                  <div className="muted">
                    {u.first_name} {u.last_name} · id: {u.id}
                  </div>
                </div>
                <div className="muted">{u.blocked ? 'blocked' : 'active'}</div>
              </div>

              <div className="row2">
                <div className="field">
                  <label>Роль</label>
                  <select
                    value={u.role}
                    onChange={(e) => updateRow(u.id, { role: e.target.value })}
                    disabled={savingId === u.id}
                    style={{
                      padding: '10px 12px',
                      borderRadius: 10,
                      border: '1px solid rgba(0,0,0,0.18)',
                      font: 'inherit',
                    }}
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>
                        {r}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <label>Blocked</label>
                  <select
                    value={String(Boolean(u.blocked))}
                    onChange={(e) => updateRow(u.id, { blocked: e.target.value === 'true' })}
                    disabled={savingId === u.id}
                    style={{
                      padding: '10px 12px',
                      borderRadius: 10,
                      border: '1px solid rgba(0,0,0,0.18)',
                      font: 'inherit',
                    }}
                  >
                    <option value="false">false</option>
                    <option value="true">true</option>
                  </select>
                </div>
              </div>

              <div className="actions">
                <button className="btn danger" onClick={() => blockRow(u.id)} disabled={savingId === u.id || u.blocked}>
                  Заблокировать
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}


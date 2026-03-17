import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import * as api from '../api/endpoints'
import { getTokens } from '../api/client'
import { useAuth } from '../state/AuthContext'

export default function ProductsPage() {
  const { user, loading } = useAuth()
  const nav = useNavigate()
  const [items, setItems] = useState([])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(true)

  const canManage = useMemo(() => Boolean(user), [user])

  useEffect(() => {
    const { accessToken } = getTokens()
    if (!accessToken) {
      nav('/login', { state: { from: '/products' } })
      return
    }
    ;(async () => {
      setBusy(true)
      setError('')
      try {
        const list = await api.listProducts()
        setItems(list || [])
      } catch (e) {
        setError(e?.response?.data?.error || 'Не удалось загрузить товары')
      } finally {
        setBusy(false)
      }
    })()
  }, [nav])

  async function onCreateClick() {
    if (!user) {
      nav('/login', { state: { from: '/products/new' } })
      return
    }
    nav('/products/new')
  }

  return (
    <div className="row">
      <div className="card row">
        <div className="actions" style={{ justifyContent: 'space-between' }}>
          <div>
            <h2 style={{ margin: 0 }}>Товары</h2>
            <div className="muted">Список доступных товаров</div>
          </div>
          <button className="btn primary" onClick={onCreateClick} disabled={loading}>
            Создать товар
          </button>
        </div>
        {busy && <div className="muted">Загрузка...</div>}
        {error && <div className="error">{error}</div>}
        {!busy && !error && items.length === 0 && <div className="muted">Пока товаров нет</div>}
        <div className="row">
          {items.map((p) => (
            <div className="card" key={p.id}>
              <div className="actions" style={{ justifyContent: 'space-between' }}>
                <div className="row" style={{ gap: 4 }}>
                  <div style={{ fontWeight: 700 }}>{p.title}</div>
                  <div className="muted">{p.category}</div>
                </div>
                <div style={{ fontWeight: 700 }}>{p.price} ₽</div>
              </div>
              <div className="actions" style={{ marginTop: 10 }}>
                <Link className="btn" to={`/products/${p.id}`}>
                  Открыть
                </Link>
                {canManage && (
                  <Link className="btn" to={`/products/${p.id}/edit`}>
                    Редактировать
                  </Link>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
      <div className="muted">
        Примечание: в рамках практики 10 управление товарами делаем через авторизацию (токены хранятся в localStorage).
      </div>
    </div>
  )
}


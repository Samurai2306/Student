import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import * as api from '../api/endpoints'
import { useAuth } from '../state/AuthContext'

export default function ProductsPage() {
  const { user, loading } = useAuth()
  const nav = useNavigate()
  const location = useLocation()
  const [items, setItems] = useState([])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(true)

  const canCreate = useMemo(() => user?.role === 'seller' || user?.role === 'admin', [user])
  const canEdit = canCreate

  useEffect(() => {
    ;(async () => {
      setBusy(true)
      setError('')
      try {
        const list = await api.listProducts()
        setItems(list || [])
      } catch (e) {
        if (e?.response?.status === 401) {
          nav('/login', { state: { from: '/products' } })
          return
        }
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
    if (!canCreate) {
      setError('Недостаточно прав для создания товара')
      return
    }
    nav('/products/new')
  }

  return (
    <div className="row">
      <div className="card row">
        <div className="actions spread">
          <div>
            <h2 className="title-no-margin">Товары</h2>
            <div className="muted">Список доступных товаров</div>
          </div>
          <button className="btn primary" onClick={onCreateClick} disabled={loading}>
            Создать товар
          </button>
        </div>
        {location.state?.created && <div className="notice">Товар успешно создан и добавлен в список.</div>}
        {busy && <div className="muted">Загрузка...</div>}
        {error && <div className="error">{error}</div>}
        {!busy && !error && items.length === 0 && <div className="muted">Пока товаров нет</div>}
        <div className="row">
          {items.map((p) => (
            <div className="card" key={p.id}>
              <div className="actions spread">
                <div className="row stack-4">
                  <div className="strong">{p.title}</div>
                  <div className="muted">{p.category}</div>
                </div>
                <div className="strong">{p.price} ₽</div>
              </div>
              <div className="actions mt-10">
                <Link className="btn" to={`/products/${p.id}`}>
                  Открыть
                </Link>
                {canEdit && (
                  <Link className="btn" to={`/products/${p.id}/edit`}>
                    Редактировать
                  </Link>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}


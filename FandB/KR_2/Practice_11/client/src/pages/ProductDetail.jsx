import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import * as api from '../api/endpoints'
import { useAuth } from '../state/AuthContext'

export default function ProductDetailPage() {
  const { id } = useParams()
  const nav = useNavigate()
  const { user } = useAuth()
  const canEdit = user?.role === 'seller' || user?.role === 'admin'

  const [product, setProduct] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(true)

  useEffect(() => {
    ;(async () => {
      setBusy(true)
      setError('')
      try {
        const p = await api.getProduct(id)
        setProduct(p)
      } catch (e) {
        if (e?.response?.status === 401) {
          nav('/login', { state: { from: `/products/${id}` } })
          return
        }
        setError(e?.response?.data?.error || 'Не удалось загрузить товар')
      } finally {
        setBusy(false)
      }
    })()
  }, [id, nav])

  if (busy) return <div className="card muted">Загрузка...</div>
  if (error) return <div className="card error">{error}</div>
  if (!product) return <div className="card muted">Не найдено</div>

  return (
    <div className="card row">
      <div className="actions" style={{ justifyContent: 'space-between' }}>
        <div>
          <h2 style={{ margin: 0 }}>{product.title}</h2>
          <div className="muted">{product.category}</div>
        </div>
        <div style={{ fontWeight: 700 }}>{product.price} ₽</div>
      </div>
      <div>{product.description}</div>
      <div className="actions">
        <Link className="btn" to="/products">
          Назад
        </Link>
        {canEdit && (
          <Link className="btn" to={`/products/${product.id}/edit`}>
            Редактировать
          </Link>
        )}
      </div>
      {!user && <div className="muted">Для просмотра/редактирования нужен вход</div>}
    </div>
  )
}


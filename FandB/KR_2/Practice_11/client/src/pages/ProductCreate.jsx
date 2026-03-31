import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import * as api from '../api/endpoints'
import { useAuth } from '../state/AuthContext'

export default function ProductCreatePage() {
  const { user } = useAuth()
  const nav = useNavigate()

  const canCreate = user?.role === 'seller' || user?.role === 'admin'

  const [title, setTitle] = useState('')
  const [category, setCategory] = useState('')
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState('0')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e) {
    e.preventDefault()
    if (!user) {
      nav('/login', { state: { from: '/products/new' } })
      return
    }
    if (!canCreate) {
      setError('Недостаточно прав для создания товара')
      return
    }
    setBusy(true)
    setError('')
    try {
      await api.createProduct({ title, category, description, price: Number(price) })
      nav('/products', { replace: true, state: { created: true } })
    } catch (e2) {
      setError(e2?.response?.data?.error || 'Не удалось создать товар')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card row">
      <div>
        <h2>Создать товар</h2>
        <div className="muted">Доступно только после входа</div>
      </div>
      <form className="row" onSubmit={onSubmit}>
        <div className="field">
          <label>Название</label>
          <input value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div className="row2">
          <div className="field">
            <label>Категория</label>
            <input value={category} onChange={(e) => setCategory(e.target.value)} />
          </div>
          <div className="field">
            <label>Цена</label>
            <input value={price} onChange={(e) => setPrice(e.target.value)} inputMode="decimal" />
          </div>
        </div>
        <div className="field">
          <label>Описание</label>
          <textarea rows={5} value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
        {error && <div className="error">{error}</div>}
        <div className="actions">
          <button className="btn primary" disabled={busy}>
            {busy ? 'Создаём...' : 'Создать'}
          </button>
          <Link className="btn" to="/products">
            Отмена
          </Link>
        </div>
      </form>
    </div>
  )
}


import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import * as api from '../api/endpoints'
import { useAuth } from '../state/AuthContext'

export default function ProductEditPage() {
  const { id } = useParams()
  const nav = useNavigate()
  const { user } = useAuth()

  const canEdit = user?.role === 'seller' || user?.role === 'admin'
  const canDelete = user?.role === 'admin'

  const [title, setTitle] = useState('')
  const [category, setCategory] = useState('')
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState('0')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!user) {
      nav('/login', { state: { from: `/products/${id}/edit` } })
      return
    }
    if (!canEdit) {
      nav('/products', { replace: true })
      return
    }
    ;(async () => {
      setBusy(true)
      setError('')
      try {
        const p = await api.getProduct(id)
        setTitle(p.title || '')
        setCategory(p.category || '')
        setDescription(p.description || '')
        setPrice(String(p.price ?? 0))
      } catch (e) {
        setError(e?.response?.data?.error || 'Не удалось загрузить товар')
      } finally {
        setBusy(false)
      }
    })()
  }, [id, nav, user])

  async function onSave(e) {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await api.updateProduct(id, { title, category, description, price: Number(price) })
      nav(`/products/${id}`, { replace: true })
    } catch (e2) {
      setError(e2?.response?.data?.error || 'Не удалось сохранить')
    } finally {
      setSaving(false)
    }
  }

  async function onDelete() {
    if (!canDelete) {
      setError('Недостаточно прав для удаления товара')
      return
    }
    if (!confirm('Удалить товар?')) return
    setSaving(true)
    setError('')
    try {
      await api.deleteProduct(id)
      nav('/products', { replace: true })
    } catch (e2) {
      setError(e2?.response?.data?.error || 'Не удалось удалить')
    } finally {
      setSaving(false)
    }
  }

  if (busy) return <div className="card muted">Загрузка...</div>

  return (
    <div className="card row">
      <div className="actions" style={{ justifyContent: 'space-between' }}>
        <div>
          <h2 style={{ margin: 0 }}>Редактирование</h2>
          <div className="muted">ID: {id}</div>
        </div>
        {canDelete && (
          <button className="btn danger" onClick={onDelete} disabled={saving}>
            Удалить
          </button>
        )}
      </div>

      <form className="row" onSubmit={onSave}>
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
          <button className="btn primary" disabled={saving}>
            {saving ? 'Сохраняем...' : 'Сохранить'}
          </button>
          <Link className="btn" to={`/products/${id}`}>
            Отмена
          </Link>
        </div>
      </form>
    </div>
  )
}


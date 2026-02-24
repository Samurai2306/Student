import React, { useEffect, useState } from 'react';

const emptyProduct = {
  name: '', category: '', description: '', price: '', quantityInStock: '', rating: '', image: '',
};

export default function ProductModal({ open, mode, initialProduct, onClose, onSubmit }) {
  const [form, setForm] = useState(emptyProduct);

  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === 'Escape') onClose();
    };
    if (open) document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [open, onClose]);

  useEffect(() => {
    if (!open) {
      setForm(emptyProduct);
      return;
    }
    if (initialProduct) {
      setForm({
        name: initialProduct.name ?? '',
        category: initialProduct.category ?? '',
        description: initialProduct.description ?? '',
        price: initialProduct.price ?? '',
        quantityInStock: initialProduct.quantityInStock ?? '',
        rating: initialProduct.rating ?? '',
        image: initialProduct.image ?? '',
      });
    } else {
      setForm(emptyProduct);
    }
  }, [open, initialProduct]);

  if (!open) return null;

  const title = mode === 'edit' ? 'Редактирование товара' : 'Добавление товара';

  const handleChange = (field) => (e) =>
    setForm((prev) => ({ ...prev, [field]: e.target.value }));

  const handleSubmit = (e) => {
    e.preventDefault();
    const name = form.name.trim();
    const category = form.category.trim();
    const price = Number(form.price);
    const quantityInStock = Number(form.quantityInStock);
    const rating = form.rating === '' ? null : Number(form.rating);
    if (!name) { alert('Введите название'); return; }
    if (!category) { alert('Введите категорию'); return; }
    if (!Number.isFinite(price) || price < 0) { alert('Некорректная цена'); return; }
    if (!Number.isInteger(quantityInStock) || quantityInStock < 0) { alert('Некорректное количество'); return; }
    onSubmit({
      id: initialProduct?.id,
      name,
      category,
      description: form.description.trim(),
      price,
      quantityInStock,
      rating: rating != null && Number.isFinite(rating) ? rating : null,
      image: form.image.trim() || null,
    });
  };

  return (
    <div className="backdrop" onMouseDown={onClose}>
      <div className="modal" onMouseDown={(e) => e.stopPropagation()} role="dialog" aria-modal="true">
        <div className="modal__header">
          <div className="modal__title">{title}</div>
          <button type="button" className="iconBtn" onClick={onClose} aria-label="Закрыть">✕</button>
        </div>
        <form className="form" onSubmit={handleSubmit}>
          <label className="label">
            Название *
            <input className="input" value={form.name} onChange={handleChange('name')} placeholder="Название товара" />
          </label>
          <label className="label">
            Категория *
            <input className="input" value={form.category} onChange={handleChange('category')} placeholder="Например, Электроника" />
          </label>
          <label className="label">
            Описание
            <input className="input" value={form.description} onChange={handleChange('description')} placeholder="Краткое описание" />
          </label>
          <label className="label">
            Цена (₽) *
            <input className="input" type="number" min="0" step="1" value={form.price} onChange={handleChange('price')} placeholder="0" />
          </label>
          <label className="label">
            Количество на складе *
            <input className="input" type="number" min="0" step="1" value={form.quantityInStock} onChange={handleChange('quantityInStock')} placeholder="0" />
          </label>
          <label className="label">
            Рейтинг (0–5)
            <input className="input" type="number" min="0" max="5" step="0.1" value={form.rating} onChange={handleChange('rating')} placeholder="Не указан" />
          </label>
          <label className="label">
            URL изображения
            <input className="input" value={form.image} onChange={handleChange('image')} placeholder="https://..." />
          </label>
          <div className="modal__footer">
            <button type="button" className="btn" onClick={onClose}>Отмена</button>
            <button type="submit" className="btn btn--primary">{mode === 'edit' ? 'Сохранить' : 'Добавить'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}

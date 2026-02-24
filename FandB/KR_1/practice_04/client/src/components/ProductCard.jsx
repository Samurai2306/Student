import React from 'react';

export default function ProductCard({ product, onEdit, onDelete }) {
  return (
    <div className="product-card">
      <div className="product-card__image-wrap">
        <img
          className="product-card__image"
          src={product.image || 'https://via.placeholder.com/400x300?text=No+image'}
          alt={product.name}
        />
      </div>
      <div className="product-card__body">
        <div className="product-card__category">{product.category}</div>
        <h3 className="product-card__title">{product.name}</h3>
        <p className="product-card__description">{product.description || '—'}</p>
        <div className="product-card__meta">
          <span className="product-card__price">{product.price.toLocaleString('ru-RU')} ₽</span>
          <span className="product-card__stock">В наличии: {product.quantityInStock}</span>
          {product.rating != null && (
            <span className="product-card__rating">★ {product.rating}</span>
          )}
        </div>
        <div className="product-card__actions">
          <button className="btn" onClick={() => onEdit(product)}>Редактировать</button>
          <button className="btn btn--danger" onClick={() => onDelete(product.id)}>Удалить</button>
        </div>
      </div>
    </div>
  );
}

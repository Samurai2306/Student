import React from 'react';
import ProductCard from './ProductCard';

export default function ProductList({ products, onEdit, onDelete, emptyMessage = 'Товаров пока нет' }) {
  if (!products.length) {
    return <div className="empty">{emptyMessage}</div>;
  }
  return (
    <div className="grid">
      {products.map((p) => (
        <ProductCard key={p.id} product={p} onEdit={onEdit} onDelete={onDelete} />
      ))}
    </div>
  );
}

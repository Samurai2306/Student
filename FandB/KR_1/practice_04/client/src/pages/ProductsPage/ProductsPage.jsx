import React, { useEffect, useMemo, useState } from 'react';
import { api } from '../../api';
import ProductList from '../../components/ProductList';
import ProductModal from '../../components/ProductModal';
import './ProductsPage.scss';

const SORT_OPTIONS = [
  { value: '', label: 'Без сортировки' },
  { value: 'price-asc', label: 'Цена: по возрастанию' },
  { value: 'price-desc', label: 'Цена: по убыванию' },
  { value: 'name-asc', label: 'Название: А–Я' },
  { value: 'name-desc', label: 'Название: Я–А' },
];

export default function ProductsPage() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState('create');
  const [editingProduct, setEditingProduct] = useState(null);
  const [categoryFilter, setCategoryFilter] = useState('');
  const [sortBy, setSortBy] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => { loadProducts(); }, []);

  const categories = useMemo(() => {
    const set = new Set(products.map((p) => p.category).filter(Boolean));
    return Array.from(set).sort();
  }, [products]);

  const filteredAndSortedProducts = useMemo(() => {
    let list = products;
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      list = list.filter((p) => p.name.toLowerCase().includes(q) || (p.description && p.description.toLowerCase().includes(q)));
    }
    if (categoryFilter) {
      list = list.filter((p) => p.category === categoryFilter);
    }
    if (sortBy) {
      const [field, order] = sortBy.split('-');
      list = [...list].sort((a, b) => {
        const aVal = field === 'price' ? a.price : (a.name || '').toLowerCase();
        const bVal = field === 'price' ? b.price : (b.name || '').toLowerCase();
        if (aVal < bVal) return order === 'asc' ? -1 : 1;
        if (aVal > bVal) return order === 'asc' ? 1 : -1;
        return 0;
      });
    }
    return list;
  }, [products, searchQuery, categoryFilter, sortBy]);

  const loadProducts = async () => {
    try {
      setLoading(true);
      const data = await api.getProducts();
      setProducts(data);
    } catch (err) {
      console.error(err);
      const msg = err.response?.data?.error || 'Ошибка загрузки товаров';
      alert(msg);
    } finally {
      setLoading(false);
    }
  };

  const openCreate = () => {
    setModalMode('create');
    setEditingProduct(null);
    setModalOpen(true);
  };

  const openEdit = (product) => {
    setModalMode('edit');
    setEditingProduct(product);
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditingProduct(null);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Удалить товар?')) return;
    try {
      await api.deleteProduct(id);
      setProducts((prev) => prev.filter((p) => p.id !== id));
    } catch (err) {
      console.error(err);
      const msg = err.response?.data?.error || 'Ошибка удаления';
      alert(msg);
    }
  };

  const handleSubmitModal = async (payload) => {
    try {
      if (modalMode === 'create') {
        const newProduct = await api.createProduct(payload);
        setProducts((prev) => [...prev, newProduct]);
      } else {
        const updated = await api.updateProduct(payload.id, payload);
        setProducts((prev) => prev.map((p) => (p.id === payload.id ? updated : p)));
      }
      closeModal();
    } catch (err) {
      console.error(err);
      const msg = err.response?.data?.error || 'Ошибка сохранения';
      alert(msg);
    }
  };

  return (
    <div className="page">
      <header className="header">
        <div className="header__inner">
          <div className="brand">Интернет-магазин</div>
          <div className="header__right">Практика 4</div>
        </div>
      </header>
      <main className="main">
        <div className="container">
          <div className="toolbar">
            <h1 className="title">Товары</h1>
            <button className="btn btn--primary" onClick={openCreate}>+ Добавить товар</button>
          </div>
          {!loading && (
            <div className="filters">
              <input
                type="text"
                className="input input--search"
                placeholder="Поиск по названию и описанию..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
              <select className="select" value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
                <option value="">Все категории</option>
                {categories.map((cat) => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
              <select className="select" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                {SORT_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          )}
          {loading ? (
            <div className="empty">Загрузка...</div>
          ) : (
            <ProductList products={filteredAndSortedProducts} onEdit={openEdit} onDelete={handleDelete} emptyMessage="По вашему запросу ничего не найдено" />
          )}
        </div>
      </main>
      <footer className="footer">
        <div className="footer__inner">© {new Date().getFullYear()} Интернет-магазин</div>
      </footer>
      <ProductModal
        open={modalOpen}
        mode={modalMode}
        initialProduct={editingProduct}
        onClose={closeModal}
        onSubmit={handleSubmitModal}
      />
    </div>
  );
}

const express = require('express');
const cors = require('cors');
const { nanoid } = require('nanoid');

const app = express();
const port = 3000;

app.use(express.json());
app.use(cors({ origin: 'http://localhost:3001', methods: ['GET', 'POST', 'PATCH', 'DELETE'], allowedHeaders: ['Content-Type'] }));

app.use((req, res, next) => {
    res.on('finish', () => {
        console.log(`[${new Date().toISOString()}] ${req.method} ${req.path} → ${res.statusCode}`);
    });
    next();
});

let products = [
    { id: nanoid(6), name: 'Ноутбук ASUS', category: 'Электроника', description: '15.6", 8 ГБ RAM, SSD 256 ГБ', price: 54990, quantityInStock: 12, rating: 4.5, image: 'https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Беспроводные наушники', category: 'Электроника', description: 'Bluetooth 5.0, шумоподавление', price: 3990, quantityInStock: 25, rating: 4.2, image: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Кофе в зёрнах', category: 'Продукты', description: 'Арабика 1 кг, средняя обжарка', price: 890, quantityInStock: 50, rating: 4.8, image: 'https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Рюкзак городской', category: 'Аксессуары', description: 'Объём 25 л, водоотталкивающая ткань', price: 2990, quantityInStock: 18, rating: 4.0, image: 'https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Часы наручные', category: 'Аксессуары', description: 'Кварцевые, водозащита 3 ATM', price: 2490, quantityInStock: 30, rating: 4.3, image: 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Книга «Чистый код»', category: 'Книги', description: 'Роберт Мартин, мягкая обложка', price: 650, quantityInStock: 40, rating: 4.9, image: 'https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Набор ручек', category: 'Канцтовары', description: '6 цветов, гелевые', price: 180, quantityInStock: 100, rating: 4.1, image: 'https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Фитнес-браслет', category: 'Электроника', description: 'Пульсометр, шагомер, уведомления', price: 1990, quantityInStock: 22, rating: 4.4, image: 'https://images.unsplash.com/photo-1576243345690-4e4b79b63288?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Чай зелёный', category: 'Продукты', description: 'Пакетированный, 25 пакетиков', price: 320, quantityInStock: 60, rating: 4.6, image: 'https://images.unsplash.com/photo-1564890369478-c89ca6d9cde9?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Коврик для мыши', category: 'Аксессуары', description: 'XL, прорезиненная основа', price: 450, quantityInStock: 35, rating: 4.0, image: 'https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Флешка 32 ГБ', category: 'Электроника', description: 'USB 3.0, металлический корпус', price: 590, quantityInStock: 45, rating: 4.2, image: 'https://images.unsplash.com/photo-1589254065878-42f9aa3c2ba2?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Монитор 24"', category: 'Электроника', description: 'Full HD, HDMI, матовый экран', price: 12990, quantityInStock: 8, rating: 4.6, image: 'https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=400&h=300&fit=crop' },
    { id: nanoid(6), name: 'Блокнот А5', category: 'Канцтовары', description: '96 листов, клетка, твёрдая обложка', price: 240, quantityInStock: 80, rating: 4.3, image: 'https://images.unsplash.com/photo-1517842645767-c639042777db?w=400&h=300&fit=crop' },
];

function findProductOr404(id, res) {
    const product = products.find(p => p.id === id);
    if (!product) {
        res.status(404).json({ error: 'Product not found' });
        return null;
    }
    return product;
}

app.get('/api/products', (req, res) => res.json(products));

app.get('/api/products/:id', (req, res) => {
    const product = findProductOr404(req.params.id, res);
    if (!product) return;
    res.json(product);
});

app.post('/api/products', (req, res) => {
    const { name, category, description, price, quantityInStock, rating, image } = req.body;
    if (!name || !category || price === undefined || quantityInStock === undefined) {
        return res.status(400).json({ error: 'Required: name, category, price, quantityInStock' });
    }
    const priceNum = Number(price);
    const qtyNum = Number(quantityInStock);
    if (!Number.isFinite(priceNum) || priceNum < 0) {
        return res.status(400).json({ error: 'Цена должна быть неотрицательным числом' });
    }
    if (!Number.isInteger(qtyNum) || qtyNum < 0) {
        return res.status(400).json({ error: 'Количество на складе — целое неотрицательное число' });
    }
    const newProduct = {
        id: nanoid(6),
        name: String(name).trim(),
        category: String(category).trim(),
        description: description ? String(description).trim() : '',
        price: priceNum,
        quantityInStock: qtyNum,
        rating: rating != null ? Number(rating) : null,
        image: image || null,
    };
    products.push(newProduct);
    res.status(201).json(newProduct);
});

app.patch('/api/products/:id', (req, res) => {
    const product = findProductOr404(req.params.id, res);
    if (!product) return;
    const { name, category, description, price, quantityInStock, rating, image } = req.body;
    if (name !== undefined) product.name = String(name).trim();
    if (category !== undefined) product.category = String(category).trim();
    if (description !== undefined) product.description = String(description).trim();
    if (price !== undefined) {
        const priceNum = Number(price);
        if (!Number.isFinite(priceNum) || priceNum < 0) {
            return res.status(400).json({ error: 'Цена должна быть неотрицательным числом' });
        }
        product.price = priceNum;
    }
    if (quantityInStock !== undefined) {
        const qtyNum = Number(quantityInStock);
        if (!Number.isInteger(qtyNum) || qtyNum < 0) {
            return res.status(400).json({ error: 'Количество на складе — целое неотрицательное число' });
        }
        product.quantityInStock = qtyNum;
    }
    if (rating !== undefined) product.rating = rating == null ? null : Number(rating);
    if (image !== undefined) product.image = image;
    res.json(product);
});

app.delete('/api/products/:id', (req, res) => {
    const exists = products.some(p => p.id === req.params.id);
    if (!exists) return res.status(404).json({ error: 'Product not found' });
    products = products.filter(p => p.id !== req.params.id);
    res.status(204).send();
});

app.use((req, res) => res.status(404).json({ error: 'Not found' }));
app.use((err, req, res, next) => {
    console.error(err);
    res.status(500).json({ error: 'Internal server error' });
});

app.listen(port, () => console.log(`Сервер: http://localhost:${port}`));

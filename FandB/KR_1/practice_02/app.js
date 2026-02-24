const express = require('express');
const app = express();
const port = 3000;

let products = [
    { id: 1, name: 'Ноутбук', cost: 45000 },
    { id: 2, name: 'Мышь', cost: 890 },
    { id: 3, name: 'Клавиатура', cost: 3200 },
];

app.use(express.json());

// Логирование запросов
app.use((req, res, next) => {
    res.on('finish', () => {
        console.log(`[${new Date().toISOString()}] ${req.method} ${req.path} → ${res.statusCode}`);
    });
    next();
});

app.get('/', (req, res) => {
    res.send('API товаров. Маршруты: GET/POST /products, GET/PATCH/DELETE /products/:id');
});

// GET /products — просмотр всех товаров
app.get('/products', (req, res) => {
    res.json(products);
});

// GET /products/:id — просмотр товара по id
app.get('/products/:id', (req, res) => {
    const product = products.find(p => p.id === Number(req.params.id));
    if (!product) return res.status(404).json({ error: 'Товар не найден' });
    res.json(product);
});

// POST /products — добавление товара
app.post('/products', (req, res) => {
    const { name, cost } = req.body;
    if (!name || cost === undefined) {
        return res.status(400).json({ error: 'Нужны поля name и cost' });
    }
    const costNum = Number(cost);
    if (!Number.isFinite(costNum) || costNum < 0) {
        return res.status(400).json({ error: 'Поле cost должно быть неотрицательным числом' });
    }
    const id = products.length ? Math.max(...products.map(p => p.id)) + 1 : 1;
    const newProduct = { id, name: String(name).trim(), cost: costNum };
    products.push(newProduct);
    res.status(201).json(newProduct);
});

// PATCH /products/:id — редактирование товара
app.patch('/products/:id', (req, res) => {
    const id = Number(req.params.id);
    const product = products.find(p => p.id === id);
    if (!product) return res.status(404).json({ error: 'Товар не найден' });
    const { name, cost } = req.body;
    if (name !== undefined) product.name = String(name).trim();
    if (cost !== undefined) {
        const costNum = Number(cost);
        if (!Number.isFinite(costNum) || costNum < 0) {
            return res.status(400).json({ error: 'Поле cost должно быть неотрицательным числом' });
        }
        product.cost = costNum;
    }
    res.json(product);
});

// DELETE /products/:id — удаление товара
app.delete('/products/:id', (req, res) => {
    const id = Number(req.params.id);
    const exists = products.some(p => p.id === id);
    if (!exists) return res.status(404).json({ error: 'Товар не найден' });
    products = products.filter(p => p.id !== id);
    res.status(204).send();
});

app.listen(port, () => {
    console.log(`Сервер запущен на http://localhost:${port}`);
});

# Практика 4 — API + React (интернет-магазин)

## Стек
- **Сервер:** Node.js, Express, CORS, nanoid
- **Клиент:** React 18, Vite, axios, Sass

```
practice_04/
├── server/
│   ├── app.js          # Express API: /api/products (GET, POST, GET/:id, PATCH/:id, DELETE/:id)
│   └── package.json
└── client/
    ├── index.html
    ├── vite.config.js
    ├── src/
    │   ├── main.jsx
    │   ├── App.jsx
    │   ├── index.css
    │   ├── api/
    │   │   └── index.js    # axios-клиент для API
    │   ├── components/
    │   │   ├── ProductCard.jsx
    │   │   ├── ProductList.jsx
    │   │   └── ProductModal.jsx
    │   └── pages/
    │       └── ProductsPage/
    │           ├── ProductsPage.jsx
    │           └── ProductsPage.scss
    └── package.json
```

## Запуск

**Терминал 1 — сервер:**
```bash
cd server
npm install
npm start
```
→ http://localhost:3000 (если порт занят: `set PORT=3002 && npm start` на Windows, тогда в `client/src/api/index.js` укажите baseURL с портом 3002)

**Терминал 2 — клиент:**
```bash
cd client
npm install
npm run dev
```
→ http://localhost:3001

# Practice 21 — демо Redis cache

```powershell
docker run -d --name redis-cache -p 6379:6379 redis
cd KR_2\Practice_11\server
$env:PORT="3020"
npm start
```

Открыть: **http://localhost:3020/cache-demo/**

Демо: два раза **GET /api/users** → MISS, затем HIT.

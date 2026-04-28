# Practice 22 — Load Balancing (Nginx + HAProxy)

## Backend

Один и тот же backend (`backend/server.js`) запускается на нескольких портах с разными `SERVER_ID`.

```powershell
cd KR_4/practice_22_load_balancer/backend
npm i

$env:PORT=3000; $env:SERVER_ID="backend-1"; npm start
$env:PORT=3001; $env:SERVER_ID="backend-2"; npm start
$env:PORT=3002; $env:SERVER_ID="backend-3"; npm start
```

## Nginx (балансировка)

Конфиг: `nginx.conf` (порт балансировщика `8080`, max_fails/fail_timeout настроены).

Пример запуска (если nginx установлен локально):

```bash
nginx -c "$(pwd)/nginx.conf"
```

Проверка:

```bash
curl.exe http://localhost:8080/
```

## HAProxy (альтернатива)

Конфиг: `haproxy.cfg` (порт `8081`).

Пример запуска (если haproxy установлен локально):

```bash
haproxy -f haproxy.cfg
```

Проверка:

```bash
curl.exe http://localhost:8081/
```


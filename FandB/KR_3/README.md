# KR_3 (Практики 13–18)

Выполнено приложение **«Заметки»** с офлайн‑режимом (Service Worker), PWA‑манифестом, архитектурой **App Shell**, WebSocket‑событиями (Socket.IO), push‑уведомлениями и напоминаниями (snooze на 5 минут).

## Запуск (Practice 13–17)

Перейдите в проект:

```bash
cd KR_3/notes-app
```

Установите зависимости и сгенерируйте PNG‑иконки для манифеста:

```bash
npm i
npm run generate:assets
```

### Push (VAPID)

Сгенерируйте VAPID‑ключи:

```bash
npx web-push generate-vapid-keys
```

Запустите сервер, передав ключи через переменные окружения:

```bash
# PowerShell пример:
$env:VAPID_PUBLIC_KEY="BCMK1rXr49bZdiapsOn-bhg2OQj0TrCYXWpSOoLD2c_E_QaUFKvnl8lIOVkW_FbJ6GUBkVdQLByGev-xtI6x00Y"
$env:VAPID_PRIVATE_KEY="LKTSKfn-48-arA-v2_IDluoPmWE3xadJyqMKJhp5hRM"
npm run dev
```

Откройте `http://localhost:3001`.

### HTTPS (Practice 15)

Для запуска по HTTPS можно использовать `mkcert` (самоподписанный сертификат) и включить HTTPS на сервере.

1. Сгенерируйте сертификаты (пример из методички):

```bash
mkcert -install
mkcert localhost 127.0.0.1 ::1
```

1. Запустите сервер:

```powershell
$env:USE_HTTPS="1"
$env:SSL_CERT_FILE=".\localhost.pem"
$env:SSL_KEY_FILE=".\localhost-key.pem"
npm run dev
```

Откройте `https://localhost:3001`.

## Проверка требований

- **Офлайн**: после первого открытия отключите сеть и обновите страницу — каркас и страницы должны загрузиться из кэша.
- **localStorage**: заметки сохраняются локально и не пропадают после перезагрузки.
- **WebSocket**: откройте 2 вкладки, добавьте заметку в одной — во второй появится toast.
- **Push**: нажмите «Включить уведомления», добавьте заметку или напоминание — должно прийти системное уведомление.
- **Snooze**: для напоминаний в уведомлении есть кнопка «Отложить на 5 минут».

## Practice 18

Это README и проверка работоспособности — часть подготовки к **КР №3**.
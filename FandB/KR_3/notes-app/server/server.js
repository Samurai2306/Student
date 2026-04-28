const path = require("node:path");
const http = require("node:http");
const https = require("node:https");
const fs = require("node:fs");

const express = require("express");
const bodyParser = require("body-parser");
const cors = require("cors");
const { Server } = require("socket.io");
const webpush = require("web-push");

const PORT = process.env.PORT ? Number(process.env.PORT) : 3001;
const USE_HTTPS = process.env.USE_HTTPS === "1" || process.env.USE_HTTPS === "true";
const SSL_CERT_FILE = process.env.SSL_CERT_FILE || "";
const SSL_KEY_FILE = process.env.SSL_KEY_FILE || "";

// VAPID keys for Push
// For учебного проекта храним в env; публичный ключ отдаём клиенту.
const VAPID_PUBLIC_KEY = process.env.VAPID_PUBLIC_KEY || "";
const VAPID_PRIVATE_KEY = process.env.VAPID_PRIVATE_KEY || "";
const VAPID_SUBJECT = process.env.VAPID_SUBJECT || "mailto:student@example.com";

if (!VAPID_PUBLIC_KEY || !VAPID_PRIVATE_KEY) {
  console.warn(
    [
      "WARNING: VAPID keys are not set.",
      "Run inside KR_3/notes-app:",
      "  npm i",
      "  npx web-push generate-vapid-keys",
      "Then set env vars VAPID_PUBLIC_KEY and VAPID_PRIVATE_KEY before запуск."
    ].join("\n")
  );
} else {
  webpush.setVapidDetails(VAPID_SUBJECT, VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY);
}

const app = express();
app.use(cors());
app.use(bodyParser.json({ limit: "200kb" }));

const publicDir = path.join(__dirname, "..", "public");

// Ensure Manifest is detected by DevTools (correct content-type + no confusing caching)
app.get("/manifest.json", (req, res) => {
  res.setHeader("Content-Type", "application/manifest+json; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache");
  res.sendFile(path.join(publicDir, "manifest.json"));
});

// Service worker should not be aggressively cached while developing
app.get("/sw.js", (req, res) => {
  res.setHeader("Cache-Control", "no-cache");
  res.sendFile(path.join(publicDir, "sw.js"));
});

app.use(express.static(publicDir));

// Push subscriptions store (in-memory for practice)
/** @type {Array<any>} */
let subscriptions = [];

// Reminder timers store
/** @type {Map<number, { timeoutId: NodeJS.Timeout, text: string, reminderTime: number }>} */
const reminders = new Map();

app.get("/api/push/public-key", (req, res) => {
  res.json({ publicKey: VAPID_PUBLIC_KEY });
});

app.post("/api/push/subscribe", (req, res) => {
  const sub = req.body;
  if (!sub?.endpoint) return res.status(400).json({ error: "Invalid subscription" });
  const exists = subscriptions.some((s) => s.endpoint === sub.endpoint);
  if (!exists) subscriptions.push(sub);
  res.status(201).json({ ok: true });
});

app.post("/api/push/unsubscribe", (req, res) => {
  const { endpoint } = req.body || {};
  if (!endpoint) return res.status(400).json({ error: "endpoint is required" });
  subscriptions = subscriptions.filter((s) => s.endpoint !== endpoint);
  res.json({ ok: true });
});

app.post("/api/reminders/snooze", (req, res) => {
  const reminderId = Number(req.query.reminderId);
  if (!Number.isFinite(reminderId) || !reminders.has(reminderId)) {
    return res.status(404).json({ error: "Reminder not found" });
  }

  const reminder = reminders.get(reminderId);
  clearTimeout(reminder.timeoutId);

  const newDelay = 5 * 60 * 1000;
  const newTimeoutId = setTimeout(() => {
    sendPushToAll({
      title: "Напоминание (после snooze)",
      body: reminder.text,
      reminderId
    });
    reminders.delete(reminderId);
  }, newDelay);

  reminders.set(reminderId, {
    timeoutId: newTimeoutId,
    text: reminder.text,
    reminderTime: Date.now() + newDelay
  });

  res.json({ ok: true });
});

function sendPushToAll(payloadObj) {
  if (!VAPID_PUBLIC_KEY || !VAPID_PRIVATE_KEY) return;

  const payload = JSON.stringify(payloadObj);
  subscriptions.forEach((sub) => {
    webpush.sendNotification(sub, payload).catch(() => {
      // ignore failures in учебном примере
    });
  });
}

if (USE_HTTPS) {
  if (!SSL_CERT_FILE || !SSL_KEY_FILE) {
    console.error("USE_HTTPS=true, but SSL_CERT_FILE/SSL_KEY_FILE are not set.");
    process.exit(1);
  }
  if (!fs.existsSync(SSL_CERT_FILE) || !fs.existsSync(SSL_KEY_FILE)) {
    console.error("HTTPS cert/key files not found:", { SSL_CERT_FILE, SSL_KEY_FILE });
    process.exit(1);
  }
}

const server = USE_HTTPS
  ? https.createServer(
      {
        cert: fs.readFileSync(SSL_CERT_FILE),
        key: fs.readFileSync(SSL_KEY_FILE)
      },
      app
    )
  : http.createServer(app);
const io = new Server(server, { cors: { origin: "*" } });

io.on("connection", (socket) => {
  socket.on("newTask", (task) => {
    io.emit("taskAdded", task);
    sendPushToAll({ title: "Новая заметка", body: task?.text || "" });
  });

  socket.on("newReminder", (reminder) => {
    const id = Number(reminder?.id);
    const text = String(reminder?.text || "");
    const reminderTime = Number(reminder?.reminderTime);
    if (!Number.isFinite(id) || !Number.isFinite(reminderTime) || !text) return;

    const delay = reminderTime - Date.now();
    if (delay <= 0) return;

    const timeoutId = setTimeout(() => {
      sendPushToAll({ title: "!!! Напоминание", body: text, reminderId: id });
      reminders.delete(id);
    }, delay);

    const prev = reminders.get(id);
    if (prev) clearTimeout(prev.timeoutId);

    reminders.set(id, { timeoutId, text, reminderTime });
  });
});

server.listen(PORT, () => {
  const proto = USE_HTTPS ? "https" : "http";
  console.log(`KR_3 notes app running on ${proto}://localhost:${PORT}`);
});


const STATIC_CACHE = "notes-static-v3";
const DYNAMIC_CACHE = "notes-dynamic-v1";

const STATIC_ASSETS = [
  "./",
  "index.html",
  "socket.io/socket.io.js",
  "app.js",
  "styles.css",
  "manifest.json",
  "content/home.html",
  "content/about.html",
  "screenshots/desktop-wide.jpg",
  "screenshots/mobile.jpg",
  "icons/icon-48.png",
  "icons/icon-96.png",
  "icons/icon-192.png",
  "icons/icon-512.png"
];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(STATIC_CACHE)
      .then((cache) => cache.addAll(STATIC_ASSETS))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(keys.filter((k) => ![STATIC_CACHE, DYNAMIC_CACHE].includes(k)).map((k) => caches.delete(k)))
      )
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);
  if (url.origin !== location.origin) return;

  if (url.pathname.startsWith("/content/")) {
    event.respondWith(
      fetch(event.request)
        .then((networkRes) => {
          const resClone = networkRes.clone();
          caches.open(DYNAMIC_CACHE).then((cache) => cache.put(event.request, resClone));
          return networkRes;
        })
        .catch(() =>
          caches.match(event.request).then((cached) => cached || caches.match("/content/home.html"))
        )
    );
    return;
  }

  // App shell: cache first
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request)));
});

self.addEventListener("push", (event) => {
  let data = { title: "Уведомление", body: "", reminderId: null };
  if (event.data) {
    try {
      data = event.data.json();
    } catch {
      data.body = String(event.data.text());
    }
  }

  const options = {
    body: data.body,
    icon: "/icons/icon-192.png",
    badge: "/icons/icon-48.png",
    data: { reminderId: data.reminderId || null }
  };

  if (data.reminderId) {
    options.actions = [{ action: "snooze", title: "Отложить на 5 минут" }];
  }

  event.waitUntil(self.registration.showNotification(data.title, options));
});

self.addEventListener("notificationclick", (event) => {
  const notification = event.notification;
  const action = event.action;

  if (action === "snooze") {
    const reminderId = notification?.data?.reminderId;
    if (reminderId) {
      event.waitUntil(
        fetch(`/api/reminders/snooze?reminderId=${encodeURIComponent(String(reminderId))}`, { method: "POST" })
          .then(() => notification.close())
          .catch(() => notification.close())
      );
      return;
    }
  }

  event.waitUntil(
    clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if ("focus" in client) return client.focus();
      }
      if (clients.openWindow) return clients.openWindow("/");
    })
  );

  notification.close();
});


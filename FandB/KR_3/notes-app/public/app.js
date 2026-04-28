const contentDiv = document.getElementById("app-content");
const homeBtn = document.getElementById("home-btn");
const aboutBtn = document.getElementById("about-btn");

const socket = io();

function setNetStatus() {
  const pill = document.getElementById("net-status");
  const text = document.getElementById("net-status-text");
  if (!pill || !text) return;
  const online = navigator.onLine;
  if (online) {
    pill.classList.remove("offline");
    text.textContent = "Онлайн";
  } else {
    pill.classList.add("offline");
    text.textContent = "Офлайн";
  }
}

window.addEventListener("online", setNetStatus);
window.addEventListener("offline", setNetStatus);
setNetStatus();

function setActiveButton(activeId) {
  [homeBtn, aboutBtn].forEach((btn) => btn.classList.remove("active"));
  document.getElementById(activeId).classList.add("active");
}

async function loadContent(page) {
  try {
    const response = await fetch(`content/${page}.html`, { cache: "no-cache" });
    const html = await response.text();
    contentDiv.innerHTML = html;

    if (page === "home") {
      initNotes();
    }
  } catch (err) {
    contentDiv.innerHTML = `<p class="is-center text-error">Ошибка загрузки страницы.</p>`;
    console.error(err);
  }
}

homeBtn.addEventListener("click", () => {
  setActiveButton("home-btn");
  loadContent("home");
});

aboutBtn.addEventListener("click", () => {
  setActiveButton("about-btn");
  loadContent("about");
});

function routeFromHash() {
  const hash = (location.hash || "").toLowerCase();
  if (hash === "#about") {
    setActiveButton("about-btn");
    loadContent("about");
    return;
  }
  setActiveButton("home-btn");
  loadContent("home");
}

window.addEventListener("hashchange", routeFromHash);
routeFromHash();

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function showToast(title, subtitle) {
  const toast = document.createElement("div");
  toast.className = "toast";
  toast.innerHTML = `<strong>${escapeHtml(title)}</strong>${subtitle ? `<small>${escapeHtml(subtitle)}</small>` : ""}`;
  document.body.appendChild(toast);
  setTimeout(() => toast.remove(), 3200);
}

function initNotes() {
  const form = document.getElementById("note-form");
  const input = document.getElementById("note-input");
  const reminderForm = document.getElementById("reminder-form");
  const reminderText = document.getElementById("reminder-text");
  const reminderTime = document.getElementById("reminder-time");
  const list = document.getElementById("notes-list");
  const empty = document.getElementById("notes-empty");
  const clearBtn = document.getElementById("clear-notes");

  function getNotes() {
    return JSON.parse(localStorage.getItem("notes") || "[]");
  }

  function setNotes(notes) {
    localStorage.setItem("notes", JSON.stringify(notes));
  }

  function loadNotes() {
    const notes = getNotes();
    if (empty) empty.style.display = notes.length ? "none" : "block";

    list.innerHTML = notes
      .slice()
      .reverse()
      .map((note) => {
        const safeText = escapeHtml(note.text);
        const reminderLine = note.reminder
          ? `<small>Напоминание: ${escapeHtml(new Date(note.reminder).toLocaleString())}</small>`
          : "";

        return `
          <li class="note-item" data-note-id="${escapeHtml(String(note.id))}">
            <div>
              <div class="note-text">${safeText}</div>
              <div class="note-meta">
                ${reminderLine}
              </div>
            </div>
            <div class="note-actions">
              <button class="icon-btn" type="button" data-action="delete" aria-label="Удалить">Удалить</button>
            </div>
          </li>
        `;
      })
      .join("");
  }

  function addNote(text, reminderTimestamp = null) {
    const notes = getNotes();
    const newNote = { id: Date.now(), text, reminder: reminderTimestamp };
    notes.push(newNote);
    setNotes(notes);
    loadNotes();

    if (reminderTimestamp) {
      socket.emit("newReminder", {
        id: newNote.id,
        text,
        reminderTime: reminderTimestamp
      });
    } else {
      socket.emit("newTask", { text, timestamp: Date.now() });
    }
  }

  function deleteNoteById(id) {
    const notes = getNotes();
    const next = notes.filter((n) => String(n.id) !== String(id));
    setNotes(next);
    loadNotes();
  }

  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const text = input.value.trim();
    if (text) {
      addNote(text);
      input.value = "";
      input.focus();
    }
  });

  reminderForm.addEventListener("submit", (e) => {
    e.preventDefault();
    const text = reminderText.value.trim();
    const datetime = reminderTime.value;
    if (text && datetime) {
      const timestamp = new Date(datetime).getTime();
      if (timestamp > Date.now()) {
        addNote(text, timestamp);
        reminderText.value = "";
        reminderTime.value = "";
        reminderText.focus();
      } else {
        alert("Дата напоминания должна быть в будущем");
      }
    }
  });

  list.addEventListener("click", (e) => {
    const btn = e.target?.closest?.("button[data-action]");
    if (!btn) return;
    const action = btn.getAttribute("data-action");
    const li = btn.closest("li[data-note-id]");
    const id = li?.getAttribute("data-note-id");
    if (!id) return;
    if (action === "delete") {
      deleteNoteById(id);
    }
  });

  if (clearBtn) {
    clearBtn.addEventListener("click", () => {
      if (!confirm("Очистить все заметки?")) return;
      setNotes([]);
      loadNotes();
    });
  }

  loadNotes();
}

// WebSocket toast for events from other clients
socket.on("taskAdded", (task) => {
  if (!task?.text) return;
  showToast("Новая заметка", String(task.text));
});

function urlBase64ToUint8Array(base64String) {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/\-/g, "+").replace(/_/g, "/");
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; i += 1) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}

async function getVapidPublicKey() {
  const res = await fetch("api/push/public-key");
  if (!res.ok) throw new Error("Failed to fetch VAPID public key");
  const data = await res.json();
  if (!data?.publicKey) throw new Error("Missing publicKey in response");
  return data.publicKey;
}

async function subscribeToPush() {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) return;
  const registration = await navigator.serviceWorker.ready;
  const publicKey = await getVapidPublicKey();
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(publicKey)
  });
  await fetch("api/push/subscribe", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(subscription)
  });
}

async function unsubscribeFromPush() {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) return;
  const registration = await navigator.serviceWorker.ready;
  const subscription = await registration.pushManager.getSubscription();
  if (!subscription) return;
  await fetch("api/push/unsubscribe", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ endpoint: subscription.endpoint })
  });
  await subscription.unsubscribe();
}

// Service Worker + push UI
if ("serviceWorker" in navigator) {
  window.addEventListener("load", async () => {
    const enableBtn = document.getElementById("enable-push");
    const disableBtn = document.getElementById("disable-push");

    try {
      const reg = await navigator.serviceWorker.register("sw.js");

      const existing = await reg.pushManager.getSubscription();
      if (existing) {
        enableBtn.style.display = "none";
        disableBtn.style.display = "inline-block";
      }

      enableBtn.addEventListener("click", async () => {
        if (Notification.permission === "denied") {
          alert("Уведомления запрещены. Разрешите их в настройках браузера.");
          return;
        }
        if (Notification.permission === "default") {
          const permission = await Notification.requestPermission();
          if (permission !== "granted") {
            alert("Необходимо разрешить уведомления.");
            return;
          }
        }
        await subscribeToPush();
        enableBtn.style.display = "none";
        disableBtn.style.display = "inline-block";
      });

      disableBtn.addEventListener("click", async () => {
        await unsubscribeFromPush();
        disableBtn.style.display = "none";
        enableBtn.style.display = "inline-block";
      });
    } catch (err) {
      console.error("SW registration failed:", err);
    }
  });
}


const TOKEN_KEY = "p21tok";

const $ = (id) => document.getElementById(id);

function base() {
  return $("apiBase").value.trim().replace(/\/$/, "") || location.origin;
}

function token() {
  return localStorage.getItem(TOKEN_KEY);
}

function setCacheBadge(value) {
  const el = $("cacheBadge");
  el.textContent = value || "—";
  el.className = value ? value.toLowerCase() : "";
}

function setMsg(text, type) {
  const el = $("authMsg");
  el.textContent = text || "";
  el.className = "msg" + (type ? " " + type : "");
}

async function request(method, path, body) {
  const headers = { Accept: "application/json" };
  if (token()) headers.Authorization = "Bearer " + token();
  if (body !== undefined) headers["Content-Type"] = "application/json";

  const t0 = performance.now();
  const res = await fetch(base() + path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined
  });
  const ms = Math.round(performance.now() - t0);

  const cache = res.headers.get("X-Cache");
  if (method === "GET" && cache) setCacheBadge(cache);
  $("metaMs").textContent = ms + " ms";

  const text = await res.text();
  let data = text;
  try {
    data = JSON.parse(text);
  } catch {}

  $("responseBody").textContent =
    typeof data === "string" ? data : JSON.stringify(data, null, 2);

  if (method === "GET" && cache) {
    const li = document.createElement("li");
    li.className = cache.toLowerCase();
    li.textContent = `${method} ${path} → ${cache} (${ms} ms)`;
    $("log").prepend(li);
  }

  if (!res.ok) throw new Error(data?.error || res.statusText);
  return data;
}

async function checkRedis() {
  const el = $("redisStatus");
  try {
    const j = await fetch(base() + "/health").then((r) => r.json());
    if (j.redis) {
      el.textContent = "Redis: подключён";
      el.className = "status ok";
    } else {
      el.textContent = "Redis: не подключён (кэш выключен)";
      el.className = "status bad";
    }
  } catch {
    el.textContent = "Сервер недоступен";
    el.className = "status bad";
  }
}

$("apiBase").value = location.origin;
checkRedis();

$("btnLogin").addEventListener("click", async () => {
  try {
    const data = await request("POST", "/api/auth/login", {
      email: $("email").value.trim(),
      password: $("password").value
    });
    localStorage.setItem(TOKEN_KEY, data.accessToken);
    setMsg(`Вошли: ${data.user.username} (${data.user.role})`, "ok");
    if (data.user?.id) $("invalidateUserId").value = data.user.id;
    checkRedis();
  } catch (e) {
    localStorage.removeItem(TOKEN_KEY);
    setMsg(e.message, "err");
  }
});

document.querySelectorAll("[data-get]").forEach((btn) => {
  btn.addEventListener("click", async () => {
    if (!token()) {
      setMsg("Сначала войдите", "err");
      return;
    }
    try {
      const data = await request("GET", btn.dataset.get);
      setMsg("Готово", "ok");
      if (Array.isArray(data) && data[0]?.id && !$("invalidateUserId").value) {
        $("invalidateUserId").value = data[0].id;
      }
    } catch (e) {
      setMsg(e.message, "err");
    }
  });
});

$("btnInvalidate").addEventListener("click", async () => {
  const id = $("invalidateUserId").value.trim();
  if (!id || !token()) return;
  try {
    await request("PUT", "/api/users/" + encodeURIComponent(id), {
      first_name: $("newFirstName").value.trim() || "Updated"
    });
    setMsg("Кэш сброшен — снова GET /api/users", "ok");
  } catch (e) {
    setMsg(e.message, "err");
  }
});

if (token()) setMsg("Токен сохранён", "ok");

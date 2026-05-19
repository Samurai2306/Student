const API_BASE = import.meta.env.VITE_API_URL || "";

async function request(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.error || res.statusText);
  return data;
}

export function apiRequest(path, options, token) {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  return request(path, { ...options, headers: { ...headers, ...(options?.headers || {}) } });
}

export const authApi = {
  register: (body) => request("/api/auth/register", { method: "POST", body: JSON.stringify(body) }),
  login: (body) => request("/api/auth/login", { method: "POST", body: JSON.stringify(body) }),
  refresh: (refreshToken) =>
    request("/api/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken }) })
};

export const postsApi = {
  feed: (token, cursor) => {
    const q = new URLSearchParams({ limit: "10" });
    if (cursor) q.set("cursor", String(cursor));
    return apiRequest(`/api/posts?${q}`, {}, token);
  },
  create: (token, content) =>
    apiRequest("/api/posts", { method: "POST", body: JSON.stringify({ content }) }, token),
  update: (token, id, content) =>
    apiRequest(`/api/posts/${id}`, { method: "PATCH", body: JSON.stringify({ content }) }, token),
  remove: (token, id) => apiRequest(`/api/posts/${id}`, { method: "DELETE" }, token),
  like: (token, id) => apiRequest(`/api/posts/${id}/like`, { method: "POST" }, token),
  unlike: (token, id) => apiRequest(`/api/posts/${id}/like`, { method: "DELETE" }, token),
  comments: (token, id) => apiRequest(`/api/posts/${id}/comments`, {}, token),
  addComment: (token, id, content) =>
    apiRequest(`/api/posts/${id}/comments`, { method: "POST", body: JSON.stringify({ content }) }, token)
};

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function isNonEmptyString(value) {
  return typeof value === "string" && value.trim().length > 0;
}

function validateEmail(email) {
  if (!isNonEmptyString(email)) return { ok: false, error: "email is required" };
  const normalized = email.trim().toLowerCase();
  if (!EMAIL_RE.test(normalized)) return { ok: false, error: "invalid email" };
  return { ok: true, value: normalized };
}

function validatePassword(password) {
  if (!isNonEmptyString(password)) return { ok: false, error: "password is required" };
  if (password.length < 6) return { ok: false, error: "password must be at least 6 characters" };
  return { ok: true, value: password };
}

function validateUsername(username) {
  if (!isNonEmptyString(username)) return { ok: false, error: "username is required" };
  const trimmed = username.trim();
  if (trimmed.length < 2 || trimmed.length > 50) {
    return { ok: false, error: "username must be 2-50 characters" };
  }
  return { ok: true, value: trimmed };
}

function validatePostContent(content) {
  if (!isNonEmptyString(content)) return { ok: false, error: "content is required" };
  const trimmed = content.trim();
  if (trimmed.length > 2000) return { ok: false, error: "content too long (max 2000)" };
  return { ok: true, value: trimmed };
}

function parsePositiveInt(value, fallback, max = 100) {
  const n = Number(value);
  if (!Number.isInteger(n) || n < 1) return fallback;
  return Math.min(n, max);
}

function parseCursor(value) {
  if (value === undefined || value === null || value === "") return null;
  const n = Number(value);
  if (!Number.isInteger(n) || n < 1) return null;
  return n;
}

module.exports = {
  EMAIL_RE,
  isNonEmptyString,
  validateEmail,
  validatePassword,
  validateUsername,
  validatePostContent,
  parsePositiveInt,
  parseCursor
};

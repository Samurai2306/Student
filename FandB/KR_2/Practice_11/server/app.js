const express = require("express");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const { nanoid } = require("nanoid");

const app = express();
app.use(express.json());

const PORT = process.env.PORT ? Number(process.env.PORT) : 3000;

const ACCESS_SECRET = process.env.ACCESS_SECRET || "access_secret";
const REFRESH_SECRET = process.env.REFRESH_SECRET || "refresh_secret";
const ACCESS_EXPIRES_IN = process.env.ACCESS_EXPIRES_IN || "15m";
const REFRESH_EXPIRES_IN = process.env.REFRESH_EXPIRES_IN || "7d";
const ADMIN_EMAIL = process.env.ADMIN_EMAIL || "admin@practice11.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || "admin123";

/** @type {Array<{id:string,email:string,first_name:string,last_name:string,passwordHash:string,role:"user"|"seller"|"admin",blocked:boolean}>} */
const users = [];
/** @type {Array<{id:string,title:string,category:string,description:string,price:number}>} */
const products = [];
/** @type {Set<string>} */
const refreshTokens = new Set();

const ROLES = /** @type {const} */ (["user", "seller", "admin"]);
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function authMiddleware(req, res, next) {
  const header = req.headers.authorization || "";
  const [scheme, token] = header.split(" ");
  if (scheme !== "Bearer" || !token) {
    return res.status(401).json({ error: "Missing or invalid Authorization header" });
  }
  try {
    const payload = jwt.verify(token, ACCESS_SECRET);
    const user = users.find((u) => u.id === payload.sub);
    if (!user) return res.status(401).json({ error: "User not found" });
    if (user.blocked) return res.status(403).json({ error: "user is blocked" });
    req.user = { sub: user.id, email: user.email, role: user.role };
    return next();
  } catch {
    return res.status(401).json({ error: "Invalid or expired token" });
  }
}

function roleMiddleware(allowedRoles) {
  return (req, res, next) => {
    if (!req.user || !allowedRoles.includes(req.user.role)) {
      return res.status(403).json({ error: "Forbidden" });
    }
    return next();
  };
}

function normalizeEmail(email) {
  return String(email || "").trim().toLowerCase();
}

function isNonEmptyString(value) {
  return typeof value === "string" && value.trim().length > 0;
}

function validateEmail(email) {
  const normalized = normalizeEmail(email);
  if (!EMAIL_RE.test(normalized)) {
    return { ok: false, error: "email must be valid" };
  }
  return { ok: true, value: normalized };
}

function validatePassword(password) {
  const raw = String(password || "");
  if (raw.length < 6) {
    return { ok: false, error: "password must be at least 6 characters" };
  }
  return { ok: true, value: raw };
}

function revokeUserRefreshTokens(userId) {
  for (const token of refreshTokens) {
    try {
      const payload = jwt.verify(token, REFRESH_SECRET);
      if (payload.sub === userId) {
        refreshTokens.delete(token);
      }
    } catch {
      refreshTokens.delete(token);
    }
  }
}

function publicUser(user) {
  return {
    id: user.id,
    email: user.email,
    first_name: user.first_name,
    last_name: user.last_name,
    role: user.role,
    blocked: user.blocked,
  };
}

function publicProduct(p) {
  return {
    id: p.id,
    title: p.title,
    category: p.category,
    description: p.description,
    price: p.price,
  };
}

function requireBodyFields(res, obj, fields) {
  const missing = fields.filter((f) => obj[f] === undefined || obj[f] === null || obj[f] === "");
  if (missing.length) {
    res.status(400).json({ error: `Missing required fields: ${missing.join(", ")}` });
    return false;
  }
  return true;
}

function seedAdminUser() {
  const normalizedAdminEmail = normalizeEmail(ADMIN_EMAIL);
  const exists = users.some((u) => u.email === normalizedAdminEmail);
  if (exists) return;

  users.push({
    id: nanoid(10),
    email: normalizedAdminEmail,
    first_name: "System",
    last_name: "Admin",
    passwordHash: bcrypt.hashSync(ADMIN_PASSWORD, 10),
    role: "admin",
    blocked: false,
  });
}

seedAdminUser();

app.get("/health", (req, res) => {
  res.json({ ok: true });
});

function generateAccessToken(user) {
  return jwt.sign(
    {
      sub: user.id,
      email: user.email,
      role: user.role,
    },
    ACCESS_SECRET,
    { expiresIn: ACCESS_EXPIRES_IN }
  );
}

function generateRefreshToken(user) {
  return jwt.sign(
    {
      sub: user.id,
      email: user.email,
      role: user.role,
    },
    REFRESH_SECRET,
    { expiresIn: REFRESH_EXPIRES_IN, jwtid: nanoid(12) }
  );
}

function getRefreshTokenFromHeaders(req) {
  const raw = req.headers["x-refresh-token"] || req.headers["refresh-token"] || "";
  if (Array.isArray(raw)) return String(raw[0] || "");
  return String(raw || "");
}

// --- Auth (Practice 8) ---
app.post("/api/auth/register", async (req, res) => {
  const { email, first_name, last_name, password } = req.body || {};
  if (!requireBodyFields(res, req.body || {}, ["email", "first_name", "last_name", "password"])) return;

  if (!isNonEmptyString(first_name) || !isNonEmptyString(last_name)) {
    return res.status(400).json({ error: "first_name and last_name must be non-empty strings" });
  }

  const emailValidation = validateEmail(email);
  if (!emailValidation.ok) {
    return res.status(400).json({ error: emailValidation.error });
  }

  const passwordValidation = validatePassword(password);
  if (!passwordValidation.ok) {
    return res.status(400).json({ error: passwordValidation.error });
  }

  const normalized = emailValidation.value;
  const exists = users.some((u) => u.email === normalized);
  if (exists) return res.status(409).json({ error: "email already exists" });

  const passwordHash = await bcrypt.hash(passwordValidation.value, 10);
  const user = {
    id: nanoid(10),
    email: normalized,
    first_name: String(first_name).trim(),
    last_name: String(last_name).trim(),
    passwordHash,
    // Guest registration can only create a regular user.
    role: "user",
    blocked: false,
  };
  users.push(user);
  res.status(201).json(publicUser(user));
});

app.post("/api/auth/login", async (req, res) => {
  const { email, password } = req.body || {};
  if (!requireBodyFields(res, req.body || {}, ["email", "password"])) return;

  const emailValidation = validateEmail(email);
  if (!emailValidation.ok) {
    return res.status(400).json({ error: emailValidation.error });
  }

  const passwordValidation = validatePassword(password);
  if (!passwordValidation.ok) {
    return res.status(400).json({ error: passwordValidation.error });
  }

  const normalized = emailValidation.value;
  const user = users.find((u) => u.email === normalized);
  if (!user) return res.status(401).json({ error: "invalid credentials" });
  if (user.blocked) return res.status(403).json({ error: "user is blocked" });

  const ok = await bcrypt.compare(passwordValidation.value, user.passwordHash);
  if (!ok) return res.status(401).json({ error: "invalid credentials" });

  const accessToken = generateAccessToken(user);
  const refreshToken = generateRefreshToken(user);
  refreshTokens.add(refreshToken);
  res.json({ accessToken, refreshToken, user: publicUser(user) });
});

app.get("/api/auth/me", authMiddleware, roleMiddleware(["user", "seller", "admin"]), (req, res) => {
  const user = users.find((u) => u.id === req.user?.sub);
  if (!user) return res.status(404).json({ error: "user not found" });
  res.json(publicUser(user));
});

app.post("/api/auth/refresh", (req, res) => {
  const refreshToken = getRefreshTokenFromHeaders(req);
  if (!refreshToken) return res.status(400).json({ error: "Missing refresh token in headers" });
  if (!refreshTokens.has(refreshToken)) return res.status(401).json({ error: "Invalid refresh token" });

  try {
    const payload = jwt.verify(refreshToken, REFRESH_SECRET);
    const user = users.find((u) => u.id === payload.sub);
    if (!user) return res.status(401).json({ error: "User not found" });
    if (user.blocked) return res.status(403).json({ error: "user is blocked" });

    // Rotation: old refresh invalidated, new pair issued
    refreshTokens.delete(refreshToken);
    const newAccessToken = generateAccessToken(user);
    const newRefreshToken = generateRefreshToken(user);
    refreshTokens.add(newRefreshToken);

    res.json({ accessToken: newAccessToken, refreshToken: newRefreshToken });
  } catch {
    refreshTokens.delete(refreshToken);
    return res.status(401).json({ error: "Invalid or expired refresh token" });
  }
});

// --- Users (Practice 11, admin only) ---
app.get("/api/users", authMiddleware, roleMiddleware(["admin"]), (req, res) => {
  res.json(users.map(publicUser));
});

app.get("/api/users/:id", authMiddleware, roleMiddleware(["admin"]), (req, res) => {
  const u = users.find((x) => x.id === req.params.id);
  if (!u) return res.status(404).json({ error: "user not found" });
  res.json(publicUser(u));
});

app.put("/api/users/:id", authMiddleware, roleMiddleware(["admin"]), async (req, res) => {
  const u = users.find((x) => x.id === req.params.id);
  if (!u) return res.status(404).json({ error: "user not found" });

  const { email, first_name, last_name, password, role, blocked } = req.body || {};

  if (email !== undefined) {
    const emailValidation = validateEmail(email);
    if (!emailValidation.ok) return res.status(400).json({ error: emailValidation.error });
    const normalized = emailValidation.value;
    const exists = users.some((x) => x.email === normalized && x.id !== u.id);
    if (exists) return res.status(409).json({ error: "email already exists" });
    u.email = normalized;
  }
  if (first_name !== undefined) {
    if (!isNonEmptyString(first_name)) return res.status(400).json({ error: "first_name must be non-empty" });
    u.first_name = String(first_name).trim();
  }
  if (last_name !== undefined) {
    if (!isNonEmptyString(last_name)) return res.status(400).json({ error: "last_name must be non-empty" });
    u.last_name = String(last_name).trim();
  }
  if (role !== undefined) {
    const nr = String(role).trim();
    if (!ROLES.includes(nr)) return res.status(400).json({ error: `role must be one of: ${ROLES.join(", ")}` });
    u.role = nr;
  }
  if (blocked !== undefined) {
    const shouldBlock = Boolean(blocked);
    if (u.id === req.user.sub && shouldBlock) {
      return res.status(400).json({ error: "admin cannot block itself" });
    }
    u.blocked = shouldBlock;
    if (shouldBlock) revokeUserRefreshTokens(u.id);
  }
  if (password !== undefined) {
    const passwordValidation = validatePassword(password);
    if (!passwordValidation.ok) return res.status(400).json({ error: passwordValidation.error });
    u.passwordHash = await bcrypt.hash(passwordValidation.value, 10);
    revokeUserRefreshTokens(u.id);
  }

  res.json(publicUser(u));
});

app.delete("/api/users/:id", authMiddleware, roleMiddleware(["admin"]), (req, res) => {
  const u = users.find((x) => x.id === req.params.id);
  if (!u) return res.status(404).json({ error: "user not found" });
  if (u.id === req.user.sub) {
    return res.status(400).json({ error: "admin cannot block itself" });
  }
  u.blocked = true;
  revokeUserRefreshTokens(u.id);
  res.json(publicUser(u));
});

// --- Products (Practice 7) ---
app.post("/api/products", authMiddleware, roleMiddleware(["seller", "admin"]), (req, res) => {
  const { title, category, description, price } = req.body || {};
  if (!requireBodyFields(res, req.body || {}, ["title", "category", "description", "price"])) return;
  if (!isNonEmptyString(title) || !isNonEmptyString(category) || !isNonEmptyString(description)) {
    return res.status(400).json({ error: "title, category and description must be non-empty strings" });
  }

  const numPrice = Number(price);
  if (!Number.isFinite(numPrice) || numPrice < 0) {
    return res.status(400).json({ error: "price must be a non-negative number" });
  }

  const product = {
    id: nanoid(10),
    title: String(title).trim(),
    category: String(category).trim(),
    description: String(description).trim(),
    price: numPrice,
  };
  products.push(product);
  res.status(201).json(publicProduct(product));
});

app.get("/api/products", authMiddleware, roleMiddleware(["user", "seller", "admin"]), (req, res) => {
  res.json(products.map(publicProduct));
});

app.get("/api/products/:id", authMiddleware, roleMiddleware(["user", "seller", "admin"]), (req, res) => {
  const p = products.find((x) => x.id === req.params.id);
  if (!p) return res.status(404).json({ error: "product not found" });
  res.json(publicProduct(p));
});

app.put("/api/products/:id", authMiddleware, roleMiddleware(["seller", "admin"]), (req, res) => {
  const p = products.find((x) => x.id === req.params.id);
  if (!p) return res.status(404).json({ error: "product not found" });

  const { title, category, description, price } = req.body || {};
  if (title !== undefined) {
    if (!isNonEmptyString(title)) return res.status(400).json({ error: "title must be non-empty" });
    p.title = String(title).trim();
  }
  if (category !== undefined) {
    if (!isNonEmptyString(category)) return res.status(400).json({ error: "category must be non-empty" });
    p.category = String(category).trim();
  }
  if (description !== undefined) {
    if (!isNonEmptyString(description)) return res.status(400).json({ error: "description must be non-empty" });
    p.description = String(description).trim();
  }
  if (price !== undefined) {
    const numPrice = Number(price);
    if (!Number.isFinite(numPrice) || numPrice < 0) {
      return res.status(400).json({ error: "price must be a non-negative number" });
    }
    p.price = numPrice;
  }
  res.json(publicProduct(p));
});

app.delete("/api/products/:id", authMiddleware, roleMiddleware(["admin"]), (req, res) => {
  const idx = products.findIndex((x) => x.id === req.params.id);
  if (idx === -1) return res.status(404).json({ error: "product not found" });
  const [deleted] = products.splice(idx, 1);
  res.json(publicProduct(deleted));
});

app.listen(PORT, () => {
  console.log(`Practice 11 server listening on http://localhost:${PORT}`);
});


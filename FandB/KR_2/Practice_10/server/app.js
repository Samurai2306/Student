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

/** @type {Array<{id:string,email:string,first_name:string,last_name:string,passwordHash:string,blocked:boolean}>} */
const users = [];
/** @type {Array<{id:string,title:string,category:string,description:string,price:number}>} */
const products = [];
/** @type {Set<string>} */
const refreshTokens = new Set();

function authMiddleware(req, res, next) {
  const header = req.headers.authorization || "";
  const [scheme, token] = header.split(" ");
  if (scheme !== "Bearer" || !token) {
    return res.status(401).json({ error: "Missing or invalid Authorization header" });
  }
  try {
    const payload = jwt.verify(token, ACCESS_SECRET);
    req.user = payload;
    next();
  } catch {
    return res.status(401).json({ error: "Invalid or expired token" });
  }
}

function normalizeEmail(email) {
  return String(email || "").trim().toLowerCase();
}

function publicUser(user) {
  return {
    id: user.id,
    email: user.email,
    first_name: user.first_name,
    last_name: user.last_name,
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

app.get("/health", (req, res) => {
  res.json({ ok: true });
});

function generateAccessToken(user) {
  return jwt.sign(
    {
      sub: user.id,
      email: user.email,
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

  const normalized = normalizeEmail(email);
  const exists = users.some((u) => u.email === normalized);
  if (exists) return res.status(409).json({ error: "email already exists" });

  const passwordHash = await bcrypt.hash(String(password), 10);
  const user = {
    id: nanoid(10),
    email: normalized,
    first_name: String(first_name).trim(),
    last_name: String(last_name).trim(),
    passwordHash,
    blocked: false,
  };
  users.push(user);
  res.status(201).json(publicUser(user));
});

app.post("/api/auth/login", async (req, res) => {
  const { email, password } = req.body || {};
  if (!requireBodyFields(res, req.body || {}, ["email", "password"])) return;

  const normalized = normalizeEmail(email);
  const user = users.find((u) => u.email === normalized);
  if (!user) return res.status(404).json({ error: "user not found" });
  if (user.blocked) return res.status(403).json({ error: "user is blocked" });

  const ok = await bcrypt.compare(String(password), user.passwordHash);
  if (!ok) return res.status(401).json({ error: "invalid credentials" });

  const accessToken = generateAccessToken(user);
  const refreshToken = generateRefreshToken(user);
  refreshTokens.add(refreshToken);
  res.json({ accessToken, refreshToken, user: publicUser(user) });
});

app.get("/api/auth/me", authMiddleware, (req, res) => {
  const userId = req.user?.sub;
  const user = users.find((u) => u.id === userId);
  if (!user) return res.status(404).json({ error: "user not found" });
  if (user.blocked) return res.status(403).json({ error: "user is blocked" });
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

// --- Products (Practice 7) ---
app.post("/api/products", (req, res) => {
  const { title, category, description, price } = req.body || {};
  if (!requireBodyFields(res, req.body || {}, ["title", "category", "description", "price"])) return;

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

app.get("/api/products", (req, res) => {
  res.json(products.map(publicProduct));
});

app.get("/api/products/:id", authMiddleware, (req, res) => {
  const p = products.find((x) => x.id === req.params.id);
  if (!p) return res.status(404).json({ error: "product not found" });
  res.json(publicProduct(p));
});

app.put("/api/products/:id", authMiddleware, (req, res) => {
  const p = products.find((x) => x.id === req.params.id);
  if (!p) return res.status(404).json({ error: "product not found" });

  const { title, category, description, price } = req.body || {};
  if (title !== undefined) p.title = String(title).trim();
  if (category !== undefined) p.category = String(category).trim();
  if (description !== undefined) p.description = String(description).trim();
  if (price !== undefined) {
    const numPrice = Number(price);
    if (!Number.isFinite(numPrice) || numPrice < 0) {
      return res.status(400).json({ error: "price must be a non-negative number" });
    }
    p.price = numPrice;
  }
  res.json(publicProduct(p));
});

app.delete("/api/products/:id", authMiddleware, (req, res) => {
  const idx = products.findIndex((x) => x.id === req.params.id);
  if (idx === -1) return res.status(404).json({ error: "product not found" });
  const [deleted] = products.splice(idx, 1);
  res.json(publicProduct(deleted));
});

app.listen(PORT, () => {
  console.log(`Practice 10 server listening on http://localhost:${PORT}`);
});


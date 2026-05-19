const express = require("express");
const bcrypt = require("bcrypt");
const { pool } = require("../db");
const { signAccessToken, signRefreshToken, verifyToken } = require("../lib/auth");
const { validateEmail, validatePassword, validateUsername } = require("../lib/validation");

function createAuthRouter({ accessSecret, refreshSecret, refreshTokens }) {
  const router = express.Router();

  router.post("/register", async (req, res) => {
    const { email, username, password } = req.body || {};
    const emailCheck = validateEmail(email);
    if (!emailCheck.ok) return res.status(400).json({ error: emailCheck.error });
    const usernameCheck = validateUsername(username);
    if (!usernameCheck.ok) return res.status(400).json({ error: usernameCheck.error });
    const passwordCheck = validatePassword(password);
    if (!passwordCheck.ok) return res.status(400).json({ error: passwordCheck.error });

    const exists = await pool.query("SELECT id FROM users WHERE email = $1", [emailCheck.value]);
    if (exists.rows[0]) return res.status(409).json({ error: "email already registered" });

    const passwordHash = await bcrypt.hash(passwordCheck.value, 10);
    const result = await pool.query(
      `INSERT INTO users (email, username, password_hash, role)
       VALUES ($1, $2, $3, 'user')
       RETURNING id, email, username, role`,
      [emailCheck.value, usernameCheck.value, passwordHash]
    );
    const user = result.rows[0];
    const accessToken = signAccessToken(user, accessSecret);
    const refreshToken = signRefreshToken(user, refreshSecret);
    refreshTokens.add(refreshToken);
    res.status(201).json({
      user: { id: user.id, email: user.email, username: user.username, role: user.role },
      accessToken,
      refreshToken
    });
  });

  router.post("/login", async (req, res) => {
    const { email, password } = req.body || {};
    const emailCheck = validateEmail(email);
    if (!emailCheck.ok) return res.status(400).json({ error: emailCheck.error });
    if (!password) return res.status(400).json({ error: "password is required" });

    const result = await pool.query(
      "SELECT id, email, username, role, password_hash FROM users WHERE email = $1",
      [emailCheck.value]
    );
    const user = result.rows[0];
    if (!user) return res.status(401).json({ error: "invalid credentials" });

    const ok = await bcrypt.compare(String(password), user.password_hash);
    if (!ok) return res.status(401).json({ error: "invalid credentials" });

    const accessToken = signAccessToken(user, accessSecret);
    const refreshToken = signRefreshToken(user, refreshSecret);
    refreshTokens.add(refreshToken);
    res.json({
      user: { id: user.id, email: user.email, username: user.username, role: user.role },
      accessToken,
      refreshToken
    });
  });

  router.post("/refresh", (req, res) => {
    const { refreshToken } = req.body || {};
    if (!refreshToken || !refreshTokens.has(refreshToken)) {
      return res.status(401).json({ error: "invalid refresh token" });
    }
    try {
      const payload = verifyToken(refreshToken, refreshSecret);
      if (payload.type !== "refresh") throw new Error("not refresh");
      pool
        .query("SELECT id, email, username, role FROM users WHERE id = $1", [Number(payload.sub)])
        .then((result) => {
          const user = result.rows[0];
          if (!user) {
            refreshTokens.delete(refreshToken);
            return res.status(401).json({ error: "user not found" });
          }
          const accessToken = signAccessToken(user, accessSecret);
          const newRefresh = signRefreshToken(user, refreshSecret);
          refreshTokens.delete(refreshToken);
          refreshTokens.add(newRefresh);
          res.json({ accessToken, refreshToken: newRefresh });
        })
        .catch(() => res.status(500).json({ error: "server error" }));
    } catch {
      refreshTokens.delete(refreshToken);
      return res.status(401).json({ error: "invalid refresh token" });
    }
  });

  return router;
}

module.exports = { createAuthRouter };

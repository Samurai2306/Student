const express = require("express");
const cors = require("cors");
const { createClient } = require("redis");
const { pool, ensureSchema } = require("./db");
const { createAuthMiddleware } = require("./middleware/auth");
const { createAuthRouter } = require("./routes/auth");
const { createPostsRouter } = require("./routes/posts");

function createApp(options = {}) {
  const {
    accessSecret = process.env.ACCESS_SECRET || "access_secret_dev",
    refreshSecret = process.env.REFRESH_SECRET || "refresh_secret_dev",
    redisUrl = process.env.REDIS_URL || "redis://127.0.0.1:6379",
    notifyUser = () => {}
  } = options;

  const app = express();
  const refreshTokens = new Set();
  const feedVersionRef = { current: 1 };

  const redisClient = createClient({
    url: redisUrl,
    socket: {
      connectTimeout: 2000,
      reconnectStrategy: () => new Error("redis disabled")
    }
  });
  let redisReady = false;

  redisClient.on("error", () => {
    redisReady = false;
  });

  async function initRedis() {
    try {
      await Promise.race([
        redisClient.connect(),
        new Promise((_, reject) => setTimeout(() => reject(new Error("timeout")), 2000))
      ]);
      redisReady = true;
    } catch {
      redisReady = false;
    }
  }

  app.use(cors());
  app.use(express.json());

  const authMiddleware = createAuthMiddleware({ accessSecret });

  app.get("/health", async (req, res) => {
    try {
      await pool.query("SELECT 1");
      res.json({ ok: true, redis: redisReady });
    } catch (e) {
      res.status(500).json({ ok: false, error: String(e.message) });
    }
  });

  app.use("/api/auth", createAuthRouter({ accessSecret, refreshSecret, refreshTokens }));
  app.use(
    "/api/posts",
    createPostsRouter({
      authMiddleware,
      redisClient,
      getRedisReady: () => redisReady,
      feedVersionRef,
      notifyUser
    })
  );

  app.init = async () => {
    await ensureSchema();
    await initRedis();
    const adminEmail = process.env.ADMIN_EMAIL || "admin@social.local";
    const adminPassword = process.env.ADMIN_PASSWORD || "admin123";
    const bcrypt = require("bcrypt");
    const existing = await pool.query("SELECT id FROM users WHERE email = $1", [adminEmail]);
    if (!existing.rows[0]) {
      const hash = await bcrypt.hash(adminPassword, 10);
      await pool.query(
        `INSERT INTO users (email, username, password_hash, role)
         VALUES ($1, $2, $3, 'admin')`,
        [adminEmail, "admin", hash]
      );
    }
  };

  app.redisClient = redisClient;
  app.getRedisReady = () => redisReady;

  return app;
}

module.exports = { createApp };

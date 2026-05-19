const express = require("express");
const { pool } = require("../db");
const { feedCacheKey, bumpFeedVersion } = require("../lib/cache");
const { validatePostContent, parsePositiveInt, parseCursor } = require("../lib/validation");

function mapPostRow(row) {
  return {
    id: row.id,
    content: row.content,
    created_at: row.created_at,
    updated_at: row.updated_at,
    author: { id: row.user_id, username: row.username },
    likes_count: Number(row.likes_count),
    comments_count: Number(row.comments_count),
    liked_by_me: Boolean(row.liked_by_me)
  };
}

async function fetchFeed(userId, cursor, limit) {
  const baseSelect = `
    SELECT p.id, p.user_id, p.content, p.created_at, p.updated_at, u.username,
           (SELECT COUNT(*)::int FROM likes l WHERE l.post_id = p.id) AS likes_count,
           (SELECT COUNT(*)::int FROM comments c WHERE c.post_id = p.id) AS comments_count,
           EXISTS(SELECT 1 FROM likes l WHERE l.post_id = p.id AND l.user_id = $1) AS liked_by_me
    FROM posts p
    JOIN users u ON u.id = p.user_id`;

  const result = cursor
    ? await pool.query(
        `${baseSelect} WHERE p.id < $2 ORDER BY p.id DESC LIMIT $3`,
        [userId, cursor, limit]
      )
    : await pool.query(`${baseSelect} ORDER BY p.id DESC LIMIT $2`, [userId, limit]);

  return result.rows.map(mapPostRow);
}

function createPostsRouter({ authMiddleware, redisClient, getRedisReady, feedVersionRef, notifyUser }) {
  const router = express.Router();

  async function invalidateFeedCache() {
    feedVersionRef.current = bumpFeedVersion(feedVersionRef.current);
    if (!getRedisReady()) return;
    try {
      const keys = await redisClient.keys("feed:v*");
      if (keys.length) await redisClient.del(keys);
    } catch {
      // ignore
    }
  }

  router.get("/", authMiddleware, async (req, res) => {
    const limit = parsePositiveInt(req.query.limit, 10, 50);
    const cursor = parseCursor(req.query.cursor);
    const cacheKey = feedCacheKey(feedVersionRef.current, cursor, limit);

    if (getRedisReady()) {
      try {
        const cached = await redisClient.get(cacheKey);
        if (cached) {
          res.setHeader("X-Cache", "HIT");
          return res.json(JSON.parse(cached));
        }
      } catch {
        // fall through
      }
    }

    const items = await fetchFeed(req.user.id, cursor, limit);
    const nextCursor = items.length === limit ? items[items.length - 1].id : null;
    const payload = { items, nextCursor };

    if (getRedisReady()) {
      try {
        await redisClient.set(cacheKey, JSON.stringify(payload), { EX: 60 });
      } catch {
        // ignore
      }
    }
    res.setHeader("X-Cache", "MISS");
    res.json(payload);
  });

  router.post("/", authMiddleware, async (req, res) => {
    const check = validatePostContent(req.body?.content);
    if (!check.ok) return res.status(400).json({ error: check.error });

    const result = await pool.query(
      `INSERT INTO posts (user_id, content) VALUES ($1, $2)
       RETURNING id, user_id, content, created_at, updated_at`,
      [req.user.id, check.value]
    );
    await invalidateFeedCache();
    const row = result.rows[0];
    res.status(201).json({
      id: row.id,
      content: row.content,
      created_at: row.created_at,
      updated_at: row.updated_at,
      author: { id: req.user.id, username: req.user.username },
      likes_count: 0,
      comments_count: 0,
      liked_by_me: false
    });
  });

  router.patch("/:id", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });
    const check = validatePostContent(req.body?.content);
    if (!check.ok) return res.status(400).json({ error: check.error });

    const existing = await pool.query("SELECT user_id FROM posts WHERE id = $1", [postId]);
    const post = existing.rows[0];
    if (!post) return res.status(404).json({ error: "post not found" });
    if (post.user_id !== req.user.id && req.user.role !== "admin") {
      return res.status(403).json({ error: "Forbidden" });
    }

    const result = await pool.query(
      `UPDATE posts SET content = $1, updated_at = NOW() WHERE id = $2
       RETURNING id, user_id, content, created_at, updated_at`,
      [check.value, postId]
    );
    await invalidateFeedCache();
    res.json({
      ...result.rows[0],
      author: { id: result.rows[0].user_id, username: req.user.username }
    });
  });

  router.delete("/:id", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });

    const existing = await pool.query("SELECT user_id FROM posts WHERE id = $1", [postId]);
    const post = existing.rows[0];
    if (!post) return res.status(404).json({ error: "post not found" });
    if (post.user_id !== req.user.id && req.user.role !== "admin") {
      return res.status(403).json({ error: "Forbidden" });
    }

    await pool.query("DELETE FROM posts WHERE id = $1", [postId]);
    await invalidateFeedCache();
    res.json({ ok: true });
  });

  router.post("/:id/like", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });

    const postRow = await pool.query(
      `SELECT p.id, p.user_id, u.username FROM posts p JOIN users u ON u.id = p.user_id WHERE p.id = $1`,
      [postId]
    );
    const post = postRow.rows[0];
    if (!post) return res.status(404).json({ error: "post not found" });

    await pool.query(
      `INSERT INTO likes (post_id, user_id) VALUES ($1, $2) ON CONFLICT DO NOTHING`,
      [postId, req.user.id]
    );
    await invalidateFeedCache();

    if (post.user_id !== req.user.id) {
      notifyUser(post.user_id, {
        type: "like",
        message: `${req.user.username} liked your post`,
        postId
      });
    }
    res.json({ ok: true });
  });

  router.delete("/:id/like", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });
    await pool.query("DELETE FROM likes WHERE post_id = $1 AND user_id = $2", [postId, req.user.id]);
    await invalidateFeedCache();
    res.json({ ok: true });
  });

  router.get("/:id/comments", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });

    const result = await pool.query(
      `SELECT c.id, c.content, c.created_at, u.id AS user_id, u.username
       FROM comments c
       JOIN users u ON u.id = c.user_id
       WHERE c.post_id = $1
       ORDER BY c.id ASC`,
      [postId]
    );
    res.json(
      result.rows.map((r) => ({
        id: r.id,
        content: r.content,
        created_at: r.created_at,
        author: { id: r.user_id, username: r.username }
      }))
    );
  });

  router.post("/:id/comments", authMiddleware, async (req, res) => {
    const postId = Number(req.params.id);
    if (!Number.isInteger(postId)) return res.status(400).json({ error: "invalid id" });
    const check = validatePostContent(req.body?.content);
    if (!check.ok) return res.status(400).json({ error: check.error });

    const postRow = await pool.query("SELECT user_id FROM posts WHERE id = $1", [postId]);
    const post = postRow.rows[0];
    if (!post) return res.status(404).json({ error: "post not found" });

    const result = await pool.query(
      `INSERT INTO comments (post_id, user_id, content) VALUES ($1, $2, $3)
       RETURNING id, content, created_at`,
      [postId, req.user.id, check.value]
    );
    await invalidateFeedCache();

    if (post.user_id !== req.user.id) {
      notifyUser(post.user_id, {
        type: "comment",
        message: `${req.user.username} commented on your post`,
        postId
      });
    }

    res.status(201).json({
      id: result.rows[0].id,
      content: result.rows[0].content,
      created_at: result.rows[0].created_at,
      author: { id: req.user.id, username: req.user.username }
    });
  });

  return router;
}

module.exports = { createPostsRouter, fetchFeed, mapPostRow };

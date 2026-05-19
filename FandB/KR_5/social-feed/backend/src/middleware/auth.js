const { pool } = require("../db");
const { extractBearerToken, verifyToken, roleAllowed } = require("../lib/auth");

function createAuthMiddleware({ accessSecret }) {
  return async function authMiddleware(req, res, next) {
    const token = extractBearerToken(req.headers.authorization);
    if (!token) return res.status(401).json({ error: "Missing Authorization header" });

    try {
      const payload = verifyToken(token, accessSecret);
      const result = await pool.query(
        "SELECT id, email, username, role FROM users WHERE id = $1",
        [Number(payload.sub)]
      );
      const user = result.rows[0];
      if (!user) return res.status(401).json({ error: "User not found" });
      req.user = {
        id: user.id,
        email: user.email,
        username: user.username,
        role: user.role
      };
      return next();
    } catch {
      return res.status(401).json({ error: "Invalid or expired token" });
    }
  };
}

function roleMiddleware(allowedRoles) {
  return (req, res, next) => {
    if (!req.user) return res.status(401).json({ error: "Unauthorized" });
    if (!roleAllowed(req.user.role, allowedRoles)) {
      return res.status(403).json({ error: "Forbidden" });
    }
    return next();
  };
}

module.exports = { createAuthMiddleware, roleMiddleware };

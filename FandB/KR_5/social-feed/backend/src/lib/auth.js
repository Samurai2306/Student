const jwt = require("jsonwebtoken");

function signAccessToken(user, secret, expiresIn = "15m") {
  return jwt.sign(
    { sub: String(user.id), email: user.email, role: user.role, username: user.username },
    secret,
    { expiresIn }
  );
}

function signRefreshToken(user, secret, expiresIn = "7d") {
  return jwt.sign({ sub: String(user.id), type: "refresh" }, secret, { expiresIn });
}

function verifyToken(token, secret) {
  return jwt.verify(token, secret);
}

function extractBearerToken(header) {
  if (!header || typeof header !== "string") return null;
  const [scheme, token] = header.split(" ");
  if (scheme !== "Bearer" || !token) return null;
  return token;
}

function roleAllowed(userRole, allowedRoles) {
  return allowedRoles.includes(userRole);
}

module.exports = {
  signAccessToken,
  signRefreshToken,
  verifyToken,
  extractBearerToken,
  roleAllowed
};

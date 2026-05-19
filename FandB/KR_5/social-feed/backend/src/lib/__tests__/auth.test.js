const jwt = require("jsonwebtoken");
const {
  signAccessToken,
  signRefreshToken,
  verifyToken,
  extractBearerToken,
  roleAllowed
} = require("../auth");

describe("auth helpers", () => {
  const user = { id: 1, email: "u@test.com", username: "u", role: "user" };
  const secret = "test_secret";

  test("sign and verify access token", () => {
    const token = signAccessToken(user, secret, "1h");
    const payload = verifyToken(token, secret);
    expect(payload.sub).toBe("1");
    expect(payload.role).toBe("user");
  });

  test("refresh token has type", () => {
    const token = signRefreshToken(user, secret);
    expect(verifyToken(token, secret).type).toBe("refresh");
  });

  test("extractBearerToken", () => {
    expect(extractBearerToken("Bearer abc")).toBe("abc");
    expect(extractBearerToken("Basic x")).toBeNull();
    expect(extractBearerToken(null)).toBeNull();
  });

  test("roleAllowed", () => {
    expect(roleAllowed("admin", ["admin", "user"])).toBe(true);
    expect(roleAllowed("user", ["admin"])).toBe(false);
  });

  test("invalid token throws", () => {
    expect(() => verifyToken("bad", secret)).toThrow();
  });
});

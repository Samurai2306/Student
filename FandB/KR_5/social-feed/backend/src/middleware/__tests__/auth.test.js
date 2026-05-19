const { roleMiddleware } = require("../auth");

describe("roleMiddleware", () => {
  test("allows matching role", () => {
    const mw = roleMiddleware(["admin"]);
    const req = { user: { role: "admin" } };
    const res = { status: jest.fn().mockReturnThis(), json: jest.fn() };
    const next = jest.fn();
    mw(req, res, next);
    expect(next).toHaveBeenCalled();
  });

  test("denies wrong role", () => {
    const mw = roleMiddleware(["admin"]);
    const req = { user: { role: "user" } };
    const res = { status: jest.fn().mockReturnThis(), json: jest.fn() };
    const next = jest.fn();
    mw(req, res, next);
    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });

  test("requires user", () => {
    const mw = roleMiddleware(["user"]);
    const req = {};
    const res = { status: jest.fn().mockReturnThis(), json: jest.fn() };
    const next = jest.fn();
    mw(req, res, next);
    expect(res.status).toHaveBeenCalledWith(401);
  });
});

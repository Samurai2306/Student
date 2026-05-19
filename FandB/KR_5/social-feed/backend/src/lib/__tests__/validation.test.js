const {
  validateEmail,
  validatePassword,
  validateUsername,
  validatePostContent,
  parsePositiveInt,
  parseCursor,
  isNonEmptyString
} = require("../validation");

describe("validation", () => {
  test("validateEmail", () => {
    expect(validateEmail("bad").ok).toBe(false);
    expect(validateEmail("a@b.co").ok).toBe(true);
    expect(validateEmail("a@b.co").value).toBe("a@b.co");
  });

  test("validatePassword", () => {
    expect(validatePassword("123").ok).toBe(false);
    expect(validatePassword("123456").ok).toBe(true);
  });

  test("validateUsername", () => {
    expect(validateUsername("a").ok).toBe(false);
    expect(validateUsername("alice").ok).toBe(true);
  });

  test("validatePostContent", () => {
    expect(validatePostContent("  ").ok).toBe(false);
    expect(validatePostContent("hello").ok).toBe(true);
  });

  test("parsePositiveInt", () => {
    expect(parsePositiveInt("x", 5)).toBe(5);
    expect(parsePositiveInt("20", 5, 10)).toBe(10);
    expect(parsePositiveInt("3", 5)).toBe(3);
  });

  test("parseCursor", () => {
    expect(parseCursor("")).toBeNull();
    expect(parseCursor("5")).toBe(5);
    expect(parseCursor("bad")).toBeNull();
  });

  test("isNonEmptyString", () => {
    expect(isNonEmptyString(" hi ")).toBe(true);
    expect(isNonEmptyString("")).toBe(false);
  });
});

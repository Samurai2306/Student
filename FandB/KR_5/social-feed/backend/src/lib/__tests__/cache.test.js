const { feedCacheKey, bumpFeedVersion } = require("../cache");

describe("cache keys", () => {
  test("feedCacheKey", () => {
    expect(feedCacheKey(1, null, 10)).toBe("feed:v1:start:10");
    expect(feedCacheKey(2, 50, 10)).toBe("feed:v2:50:10");
  });

  test("bumpFeedVersion", () => {
    expect(bumpFeedVersion(0)).toBe(1);
    expect(bumpFeedVersion(3)).toBe(4);
  });
});

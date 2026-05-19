const FEED_PREFIX = "feed:v";

function feedCacheKey(version, cursor, limit) {
  return `${FEED_PREFIX}${version}:${cursor ?? "start"}:${limit}`;
}

function bumpFeedVersion(current) {
  return (current || 0) + 1;
}

module.exports = {
  FEED_PREFIX,
  feedCacheKey,
  bumpFeedVersion
};

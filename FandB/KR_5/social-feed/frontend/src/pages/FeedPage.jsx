import { useCallback, useEffect, useRef, useState } from "react";
import { io } from "socket.io-client";
import { postsApi } from "../api.js";
import { useAuth } from "../context/AuthContext.jsx";
import PostCard from "../components/PostCard.jsx";

export default function FeedPage() {
  const { user, accessToken, logout, refreshAccess } = useAuth();
  const [posts, setPosts] = useState([]);
  const [cursor, setCursor] = useState(null);
  const [hasMore, setHasMore] = useState(true);
  const [newPost, setNewPost] = useState("");
  const [loading, setLoading] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const sentinelRef = useRef(null);
  const loadingRef = useRef(false);
  const cursorRef = useRef(null);

  const loadFeed = useCallback(
    async (reset = false) => {
      if (loadingRef.current) return;
      loadingRef.current = true;
      setLoading(true);
      try {
        let token = accessToken;
        const pageCursor = reset ? null : cursorRef.current;
        let data;
        try {
          data = await postsApi.feed(token, pageCursor);
        } catch {
          token = await refreshAccess();
          data = await postsApi.feed(token, pageCursor);
        }
        cursorRef.current = data.nextCursor;
        setCursor(data.nextCursor);
        setHasMore(Boolean(data.nextCursor));
        setPosts((prev) => (reset ? data.items : [...prev, ...data.items]));
      } finally {
        loadingRef.current = false;
        setLoading(false);
      }
    },
    [accessToken, refreshAccess]
  );

  useEffect(() => {
    loadFeed(true);
  }, [loadFeed]);

  useEffect(() => {
    const socket = io({ auth: { token: accessToken } });
    socket.on("notification", (payload) => {
      setNotifications((n) => [{ ...payload, id: Date.now() + Math.random() }, ...n].slice(0, 5));
    });
    return () => socket.disconnect();
  }, [accessToken]);

  useEffect(() => {
    if (!sentinelRef.current || !hasMore) return;
    const observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting && hasMore && !loadingRef.current) loadFeed(false);
    });
    observer.observe(sentinelRef.current);
    return () => observer.disconnect();
  }, [hasMore, loadFeed]);

  async function createPost(e) {
    e.preventDefault();
    if (!newPost.trim()) return;
    await postsApi.create(accessToken, newPost.trim());
    setNewPost("");
    await loadFeed(true);
  }

  return (
    <div className="feed-page">
      <header className="topbar">
        <div>
          <h1>Лента</h1>
          <p className="muted">
            {user.username} ({user.role})
          </p>
        </div>
        <button type="button" onClick={logout}>
          Выйти
        </button>
      </header>

      {notifications.length > 0 && (
        <div className="toasts">
          {notifications.map((n) => (
            <div key={n.id} className="toast">
              {n.message}
            </div>
          ))}
        </div>
      )}

      <form className="composer card" onSubmit={createPost}>
        <textarea
          value={newPost}
          onChange={(e) => setNewPost(e.target.value)}
          placeholder="Что нового?"
          rows={2}
        />
        <button type="submit">Опубликовать</button>
      </form>

      <div className="posts">
        {posts.map((p) => (
          <PostCard
            key={p.id}
            post={p}
            token={accessToken}
            currentUser={user}
            onUpdate={() => loadFeed(true)}
          />
        ))}
      </div>

      {loading && <p className="center muted">Загрузка…</p>}
      {hasMore && <div ref={sentinelRef} className="sentinel" />}
      {!hasMore && posts.length > 0 && <p className="center muted">Конец ленты</p>}
    </div>
  );
}

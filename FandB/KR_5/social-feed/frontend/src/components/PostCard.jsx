import { useState } from "react";
import { postsApi } from "../api.js";

export default function PostCard({ post, token, currentUser, onUpdate }) {
  const [editing, setEditing] = useState(false);
  const [content, setContent] = useState(post.content);
  const [commentsOpen, setCommentsOpen] = useState(false);
  const [comments, setComments] = useState([]);
  const [commentText, setCommentText] = useState("");
  const [busy, setBusy] = useState(false);

  const canEdit = currentUser.id === post.author.id || currentUser.role === "admin";

  async function toggleLike() {
    setBusy(true);
    try {
      if (post.liked_by_me) await postsApi.unlike(token, post.id);
      else await postsApi.like(token, post.id);
      onUpdate();
    } finally {
      setBusy(false);
    }
  }

  async function saveEdit() {
    setBusy(true);
    try {
      await postsApi.update(token, post.id, content);
      setEditing(false);
      onUpdate();
    } finally {
      setBusy(false);
    }
  }

  async function removePost() {
    if (!confirm("Удалить пост?")) return;
    await postsApi.remove(token, post.id);
    onUpdate();
  }

  async function loadComments() {
    const list = await postsApi.comments(token, post.id);
    setComments(list);
    setCommentsOpen(true);
  }

  async function submitComment(e) {
    e.preventDefault();
    if (!commentText.trim()) return;
    await postsApi.addComment(token, post.id, commentText.trim());
    setCommentText("");
    const list = await postsApi.comments(token, post.id);
    setComments(list);
    onUpdate();
  }

  return (
    <article className="post-card">
      <header>
        <strong>{post.author.username}</strong>
        <span className="muted">{new Date(post.created_at).toLocaleString("ru-RU")}</span>
      </header>
      {editing ? (
        <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={3} />
      ) : (
        <p>{post.content}</p>
      )}
      <footer>
        <button type="button" onClick={toggleLike} disabled={busy}>
          {post.liked_by_me ? "♥" : "♡"} {post.likes_count}
        </button>
        <button type="button" onClick={() => (commentsOpen ? setCommentsOpen(false) : loadComments())}>
          💬 {post.comments_count}
        </button>
        {canEdit && !editing && (
          <>
            <button type="button" onClick={() => setEditing(true)}>
              Изменить
            </button>
            <button type="button" className="danger" onClick={removePost}>
              Удалить
            </button>
          </>
        )}
        {editing && (
          <>
            <button type="button" onClick={saveEdit} disabled={busy}>
              Сохранить
            </button>
            <button type="button" onClick={() => setEditing(false)}>
              Отмена
            </button>
          </>
        )}
      </footer>
      {commentsOpen && (
        <section className="comments">
          <ul>
            {comments.map((c) => (
              <li key={c.id}>
                <strong>{c.author.username}</strong>: {c.content}
              </li>
            ))}
          </ul>
          <form onSubmit={submitComment}>
            <input
              value={commentText}
              onChange={(e) => setCommentText(e.target.value)}
              placeholder="Комментарий…"
            />
            <button type="submit">Отправить</button>
          </form>
        </section>
      )}
    </article>
  );
}

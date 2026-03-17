import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../state/AuthContext'
import './layout.css'

export default function Layout() {
  const { user, loading, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="topbar">
        <Link className="brand" to="/">
          KR2 · Practice 11
        </Link>
        <nav className="nav">
          <NavLink to="/products">Товары</NavLink>
          {!loading && user?.role === 'admin' && <NavLink to="/users">Пользователи</NavLink>}
          {!loading && !user && (
            <>
              <NavLink to="/register">Регистрация</NavLink>
              <NavLink to="/login">Вход</NavLink>
            </>
          )}
          {!loading && user && (
            <>
              <span className="userpill">
                {user.email} · {user.role}
              </span>
              <button className="linkbtn" onClick={logout}>
                Выйти
              </button>
            </>
          )}
        </nav>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}


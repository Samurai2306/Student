import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { authApi } from "../api.js";

const AuthContext = createContext(null);
const STORAGE_KEY = "kr5_auth";

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [accessToken, setAccessToken] = useState(null);
  const [refreshToken, setRefreshToken] = useState(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const data = JSON.parse(raw);
        setUser(data.user);
        setAccessToken(data.accessToken);
        setRefreshToken(data.refreshToken);
      }
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
    setReady(true);
  }, []);

  function persist(next) {
    setUser(next.user);
    setAccessToken(next.accessToken);
    setRefreshToken(next.refreshToken);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  async function login(credentials) {
    const data = await authApi.login(credentials);
    persist(data);
    return data;
  }

  async function register(payload) {
    const data = await authApi.register(payload);
    persist(data);
    return data;
  }

  function logout() {
    setUser(null);
    setAccessToken(null);
    setRefreshToken(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  async function refreshAccess() {
    if (!refreshToken) throw new Error("no refresh");
    const data = await authApi.refresh(refreshToken);
    const next = { user, accessToken: data.accessToken, refreshToken: data.refreshToken };
    persist(next);
    return data.accessToken;
  }

  const value = useMemo(
    () => ({ user, accessToken, refreshToken, ready, login, register, logout, refreshAccess }),
    [user, accessToken, refreshToken, ready]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth outside provider");
  return ctx;
}

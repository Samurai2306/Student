import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { clearTokens, getTokens } from '../api/client'
import * as api from '../api/endpoints'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  const refreshUser = useCallback(async () => {
    const { accessToken } = getTokens()
    if (!accessToken) {
      setUser(null)
      return null
    }
    try {
      const u = await api.me()
      setUser(u)
      return u
    } catch {
      clearTokens()
      setUser(null)
      return null
    }
  }, [])

  useEffect(() => {
    ;(async () => {
      try {
        await refreshUser()
      } finally {
        setLoading(false)
      }
    })()
  }, [refreshUser])

  const doLogin = useCallback(async ({ email, password }) => {
    const res = await api.login({ email, password })
    const u = await refreshUser()
    return { ...res, user: u }
  }, [refreshUser])

  const doRegister = useCallback(async (payload) => {
    return await api.register(payload)
  }, [])

  const doLogout = useCallback(async () => {
    await api.logout()
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({
      user,
      loading,
      login: doLogin,
      register: doRegister,
      logout: doLogout,
      refreshUser,
    }),
    [user, loading, doLogin, doRegister, doLogout, refreshUser]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}


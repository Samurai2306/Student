import axios from 'axios'

const ACCESS_KEY = 'accessToken'
const REFRESH_KEY = 'refreshToken'

export function getTokens() {
  return {
    accessToken: localStorage.getItem(ACCESS_KEY) || '',
    refreshToken: localStorage.getItem(REFRESH_KEY) || '',
  }
}

export function setTokens({ accessToken, refreshToken }) {
  if (accessToken) localStorage.setItem(ACCESS_KEY, accessToken)
  if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken)
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_KEY)
  localStorage.removeItem(REFRESH_KEY)
}

export const api = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json',
    accept: 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const { accessToken } = getTokens()
  if (accessToken) {
    config.headers = config.headers || {}
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
    const status = error?.response?.status

    if (status !== 401 || original?._retry) {
      return Promise.reject(error)
    }

    const { refreshToken } = getTokens()
    if (!refreshToken) {
      clearTokens()
      return Promise.reject(error)
    }

    original._retry = true
    try {
      const refreshRes = await axios.post(
        '/api/auth/refresh',
        {},
        { headers: { 'x-refresh-token': refreshToken } }
      )
      const { accessToken: newAccess, refreshToken: newRefresh } = refreshRes.data || {}
      if (!newAccess || !newRefresh) {
        clearTokens()
        return Promise.reject(error)
      }

      setTokens({ accessToken: newAccess, refreshToken: newRefresh })
      original.headers = original.headers || {}
      original.headers.Authorization = `Bearer ${newAccess}`
      return api(original)
    } catch (e) {
      clearTokens()
      return Promise.reject(e)
    }
  }
)


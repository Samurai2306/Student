import { api, setTokens, clearTokens } from './client'

export async function register(payload) {
  const res = await api.post('/api/auth/register', payload)
  return res.data
}

export async function login(payload) {
  const res = await api.post('/api/auth/login', payload)
  const { accessToken, refreshToken } = res.data || {}
  if (accessToken && refreshToken) setTokens({ accessToken, refreshToken })
  return res.data
}

export async function logout() {
  clearTokens()
}

export async function me() {
  const res = await api.get('/api/auth/me')
  return res.data
}

export async function listProducts() {
  const res = await api.get('/api/products')
  return res.data
}

export async function getProduct(id) {
  const res = await api.get(`/api/products/${id}`)
  return res.data
}

export async function createProduct(payload) {
  const res = await api.post('/api/products', payload)
  return res.data
}

export async function updateProduct(id, payload) {
  const res = await api.put(`/api/products/${id}`, payload)
  return res.data
}

export async function deleteProduct(id) {
  const res = await api.delete(`/api/products/${id}`)
  return res.data
}

export async function listUsers() {
  const res = await api.get('/api/users')
  return res.data
}

export async function getUser(id) {
  const res = await api.get(`/api/users/${id}`)
  return res.data
}

export async function updateUser(id, payload) {
  const res = await api.put(`/api/users/${id}`, payload)
  return res.data
}

export async function blockUser(id) {
  const res = await api.delete(`/api/users/${id}`)
  return res.data
}


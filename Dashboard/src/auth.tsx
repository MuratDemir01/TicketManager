import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api } from './api'
import type { AuthUser, UserRole } from './types'

interface AuthContextValue {
  user: AuthUser | null
  isAdmin: boolean
  isEmployee: boolean
  login: (userNameOrEmail: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

const STORAGE_KEY = 'tm_auth'

function loadUser(): AuthUser | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    const user = JSON.parse(raw) as AuthUser
    if (user?.token) {
      localStorage.setItem('tm_token', user.token)
      return user
    }
  } catch {
    /* ignore */
  }
  return null
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => loadUser())

  const login = useCallback(async (userNameOrEmail: string, password: string) => {
    const result = await api.login(userNameOrEmail, password)
    localStorage.setItem('tm_token', result.token)
    localStorage.setItem(STORAGE_KEY, JSON.stringify(result))
    setUser(result)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('tm_token')
    localStorage.removeItem(STORAGE_KEY)
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({
      user,
      isAdmin: user?.role === 'Admin',
      isEmployee: user?.role === 'Employee',
      login,
      logout,
    }),
    [user, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth AuthProvider içinde kullanılmalı')
  return ctx
}

export function roleLabel(role: UserRole) {
  return role === 'Admin' ? 'Yönetici' : 'Çalışan'
}

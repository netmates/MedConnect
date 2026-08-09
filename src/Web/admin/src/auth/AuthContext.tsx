import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import type { User } from 'oidc-client-ts'
import { userManager } from './userManager'
import { getRealmRolesFromAccessToken, hasAdminRole } from './roles'

type AuthState = {
  user: User | null
  roles: string[]
  isAdmin: boolean
  isLoading: boolean
  login: () => Promise<void>
  logout: () => Promise<void>
  getAccessToken: () => Promise<string | null>
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const syncUser = useCallback(async () => {
    const current = await userManager.getUser()
    setUser(current && !current.expired ? current : null)
  }, [])

  useEffect(() => {
    let cancelled = false

    ;(async () => {
      try {
        await syncUser()
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    })()

    const onLoaded = (u: User) => setUser(u)
    const onUnloaded = () => setUser(null)

    userManager.events.addUserLoaded(onLoaded)
    userManager.events.addUserUnloaded(onUnloaded)

    return () => {
      cancelled = true
      userManager.events.removeUserLoaded(onLoaded)
      userManager.events.removeUserUnloaded(onUnloaded)
    }
  }, [syncUser])

  const roles = useMemo(
    () => getRealmRolesFromAccessToken(user?.access_token),
    [user?.access_token],
  )

  const login = useCallback(async () => {
    await userManager.signinRedirect()
  }, [])

  const logout = useCallback(async () => {
    await userManager.signoutRedirect()
  }, [])

  const getAccessToken = useCallback(async () => {
    const current = await userManager.getUser()
    if (!current || current.expired) return null
    return current.access_token
  }, [])

  const value = useMemo<AuthState>(
    () => ({
      user,
      roles,
      isAdmin: hasAdminRole(roles),
      isLoading,
      login,
      logout,
      getAccessToken,
    }),
    [user, roles, isLoading, login, logout, getAccessToken],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

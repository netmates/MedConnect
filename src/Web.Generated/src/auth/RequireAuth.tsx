import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}

export function RequireAdmin() {
  const { isAdmin, isLoading } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (!isAdmin) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

export function RequireDoctor() {
  const { isDoctor, isLoading } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (!isDoctor) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

export function RequirePatient() {
  const { isPatient, isLoading } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (!isPatient) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

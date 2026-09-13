import { Box, CircularProgress, Typography } from '@mui/material'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'

function SessionCheck() {
  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '40vh' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <CircularProgress size={22} />
        <Typography>Проверка сессии…</Typography>
      </Box>
    </Box>
  )
}

export function RequireAuth() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <SessionCheck />
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}

export function RequireAdmin() {
  const { isAdmin, isLoading } = useAuth()

  if (isLoading) {
    return <SessionCheck />
  }

  if (!isAdmin) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

export function RequireDoctor() {
  const { isDoctor, isLoading } = useAuth()

  if (isLoading) {
    return <SessionCheck />
  }

  if (!isDoctor) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

export function RequirePatient() {
  const { isPatient, isLoading } = useAuth()

  if (isLoading) {
    return <SessionCheck />
  }

  if (!isPatient) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}

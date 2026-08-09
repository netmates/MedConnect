import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { RequireAdmin, RequireAuth } from '../auth/RequireAuth'
import { AdminLayout } from './AdminLayout'
import { AccessDeniedPage } from '../pages/AccessDeniedPage'
import { AuthCallbackPage } from '../pages/AuthCallbackPage'
import { DoctorsPage } from '../pages/DoctorsPage'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { PatientDetailPage } from '../pages/PatientDetailPage'
import { PatientsPage } from '../pages/PatientsPage'
import { SpecializationsPage } from '../pages/SpecializationsPage'

function LoginRoute() {
  const { user, isLoading, isAdmin } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (user) {
    return <Navigate to={isAdmin ? '/' : '/access-denied'} replace />
  }

  return <LoginPage />
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginRoute />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/access-denied" element={<AccessDeniedPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<RequireAdmin />}>
          <Route element={<AdminLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/specializations" element={<SpecializationsPage />} />
            <Route path="/doctors" element={<DoctorsPage />} />
            <Route path="/patients" element={<PatientsPage />} />
            <Route path="/patients/:id" element={<PatientDetailPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

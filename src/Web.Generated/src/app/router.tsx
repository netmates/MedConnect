import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import {
  RequireAdmin,
  RequireAuth,
  RequireDoctor,
  RequirePatient,
} from '../auth/RequireAuth'
import { homePathForRoles } from '../auth/roles'
import { AdminLayout } from './layouts/AdminLayout'
import { DoctorLayout } from './layouts/DoctorLayout'
import { PatientLayout } from './layouts/PatientLayout'
import { ChatPage } from '../features/chat/ChatPage'
import { AdminHomePage } from '../features/admin/pages/AdminHomePage'
import { DoctorsPage } from '../features/admin/pages/DoctorsPage'
import { PatientDetailPage } from '../features/admin/pages/PatientDetailPage'
import { PatientsPage } from '../features/admin/pages/PatientsPage'
import { SpecializationsPage } from '../features/admin/pages/SpecializationsPage'
import { DoctorAppointmentsPage } from '../features/doctor/pages/DoctorAppointmentsPage'
import { DoctorHomePage } from '../features/doctor/pages/DoctorHomePage'
import { DoctorSchedulePage } from '../features/doctor/pages/DoctorSchedulePage'
import { PatientAppointmentsPage } from '../features/patient/pages/PatientAppointmentsPage'
import { PatientDoctorsPage } from '../features/patient/pages/PatientDoctorsPage'
import { PatientHomePage } from '../features/patient/pages/PatientHomePage'
import { PatientProfilePage } from '../features/patient/pages/PatientProfilePage'
import { AccessDeniedPage } from '../pages/AccessDeniedPage'
import { AuthCallbackPage } from '../pages/AuthCallbackPage'
import { LoginPage } from '../pages/LoginPage'

function LoginRoute() {
  const { user, isLoading, roles } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  if (user) {
    return <Navigate to={homePathForRoles(roles)} replace />
  }

  return <LoginPage />
}

function RootRedirect() {
  const { roles, isLoading } = useAuth()

  if (isLoading) {
    return <p className="page-status">Проверка сессии…</p>
  }

  return <Navigate to={homePathForRoles(roles)} replace />
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginRoute />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/access-denied" element={<AccessDeniedPage />} />

      <Route element={<RequireAuth />}>
        <Route path="/" element={<RootRedirect />} />

        <Route element={<RequireAdmin />}>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<AdminHomePage />} />
            <Route path="specializations" element={<SpecializationsPage />} />
            <Route path="doctors" element={<DoctorsPage />} />
            <Route path="patients" element={<PatientsPage />} />
            <Route path="patients/:id" element={<PatientDetailPage />} />
          </Route>
        </Route>

        <Route element={<RequirePatient />}>
          <Route path="/patient" element={<PatientLayout />}>
            <Route index element={<PatientHomePage />} />
            <Route path="profile" element={<PatientProfilePage />} />
            <Route path="doctors" element={<PatientDoctorsPage />} />
            <Route path="appointments" element={<PatientAppointmentsPage />} />
            <Route
              path="appointments/:appointmentId/chat"
              element={
                <ChatPage backTo="/patient/appointments" backLabel="← К записям" />
              }
            />
          </Route>
        </Route>

        <Route element={<RequireDoctor />}>
          <Route path="/doctor" element={<DoctorLayout />}>
            <Route index element={<DoctorHomePage />} />
            <Route path="schedule" element={<DoctorSchedulePage />} />
            <Route path="appointments" element={<DoctorAppointmentsPage />} />
            <Route
              path="appointments/:appointmentId/chat"
              element={
                <ChatPage backTo="/doctor/appointments" backLabel="← К приемам" />
              }
            />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

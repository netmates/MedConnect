import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export function DoctorLayout() {
  const { user, logout } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    user?.profile?.sub ??
    'user'

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          <Link to="/doctor">MedConnect · Врач</Link>
        </div>
        <nav className="nav">
          <Link to="/doctor">Главная</Link>
          <Link to="/doctor/schedule">Расписание</Link>
          <Link to="/doctor/appointments">Приемы</Link>
        </nav>
        <div className="topbar-user">
          <span>{name}</span>
          <button type="button" className="btn btn-ghost" onClick={() => void logout()}>
            Выйти
          </button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}

import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export function PatientLayout() {
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
          <Link to="/patient">MedConnect · Пациент</Link>
        </div>
        <nav className="nav">
          <Link to="/patient">Главная</Link>
          <Link to="/patient/profile">Профиль</Link>
          <Link to="/patient/doctors">Врачи</Link>
          <Link to="/patient/appointments">Записи</Link>
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

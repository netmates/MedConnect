import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export function AdminLayout() {
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
          <Link to="/admin">MedConnect · Админ</Link>
        </div>
        <nav className="nav">
          <Link to="/admin">Главная</Link>
          <Link to="/admin/specializations">Специализации</Link>
          <Link to="/admin/doctors">Врачи</Link>
          <Link to="/admin/patients">Пациенты</Link>
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

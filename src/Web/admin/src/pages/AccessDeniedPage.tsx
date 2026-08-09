import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AccessDeniedPage() {
  const { roles, logout } = useAuth()

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Нет доступа</h1>
        <p className="lead">
          Для админки нужна роль <code>admin</code> в Keycloak. Сейчас:{' '}
          {roles.length > 0 ? roles.join(', ') : 'ролей нет'}.
        </p>
        <div className="btn-row">
          <Link className="btn btn-ghost" to="/login">
            На вход
          </Link>
          <button type="button" className="btn btn-primary" onClick={() => void logout()}>
            Сменить пользователя
          </button>
        </div>
      </div>
    </div>
  )
}

import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { homePathForRoles } from '../auth/roles'

export function AccessDeniedPage() {
  const { roles, logout, user } = useAuth()
  const home = homePathForRoles(roles)

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Нет доступа</h1>
        <p className="lead">
          Этот раздел недоступен для текущей роли. Сейчас:{' '}
          {roles.length > 0 ? roles.join(', ') : 'ролей нет'}.
        </p>
        <div className="btn-row">
          {user && home !== '/access-denied' && (
            <Link className="btn btn-ghost" to={home}>
              В мой раздел
            </Link>
          )}
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

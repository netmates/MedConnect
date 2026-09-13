import { Link } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'

export function AdminHomePage() {
  const { user, roles } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <section className="panel">
      <h1>Админ-панель</h1>
      <p className="lead">
        Вы вошли как <strong>{name}</strong>.
      </p>
      <p>Роли: {roles.length > 0 ? roles.join(', ') : '—'}</p>
      <ul className="home-links">
        <li>
          <Link to="/admin/specializations">Специализации</Link>
        </li>
        <li>
          <Link to="/admin/doctors">Врачи</Link>
        </li>
        <li>
          <Link to="/admin/patients">Пациенты</Link>
        </li>
      </ul>
    </section>
  )
}

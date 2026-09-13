import { Link } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'

export function PatientHomePage() {
  const { user, roles } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <section className="panel">
      <h1>Кабинет пациента</h1>
      <p className="lead">
        Здравствуйте, <strong>{name}</strong>.
      </p>
      <p>Роли: {roles.join(', ') || '—'}</p>
      <ul className="home-links">
        <li>
          <Link to="/patient/profile">Профиль</Link>
        </li>
        <li>
          <Link to="/patient/doctors">Найти врача и записаться</Link>
        </li>
        <li>
          <Link to="/patient/appointments">Мои записи</Link>
        </li>
      </ul>
    </section>
  )
}

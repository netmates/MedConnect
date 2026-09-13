import { Link } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'

export function DoctorHomePage() {
  const { user, roles } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <section className="panel">
      <h1>Кабинет врача</h1>
      <p className="lead">
        Здравствуйте, <strong>{name}</strong>.
      </p>
      <p>Роли: {roles.join(', ') || '—'}</p>
      <ul className="home-links">
        <li>
          <Link to="/doctor/schedule">Расписание слотов</Link>
        </li>
        <li>
          <Link to="/doctor/appointments">Приемы</Link>
        </li>
      </ul>
    </section>
  )
}

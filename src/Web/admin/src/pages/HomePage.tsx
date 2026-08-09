import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function HomePage() {
  const { user, roles, isAdmin } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <section className="panel">
      <h1>Главная</h1>
      <p className="lead">
        Вы вошли как <strong>{name}</strong>.
      </p>
      <p>
        Роли: {roles.length > 0 ? roles.join(', ') : '—'}
      </p>
      {isAdmin ? (
        <ul className="home-links">
          <li>
            <Link to="/specializations">Специализации</Link>
          </li>
          <li>
            <Link to="/doctors">Врачи</Link>
          </li>
          <li>
            <Link to="/patients">Пациенты</Link>
          </li>
        </ul>
      ) : (
        <p className="warn">
          Нет роли admin — разделы управления недоступны.{' '}
          <Link to="/access-denied">Подробнее</Link>
        </p>
      )}
    </section>
  )
}

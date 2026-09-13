import CategoryIcon from '@mui/icons-material/Category'
import MedicalServicesIcon from '@mui/icons-material/MedicalServices'
import PeopleIcon from '@mui/icons-material/People'
import { List, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'
import { Page } from '../../../components/Page'

export function AdminHomePage() {
  const { user, roles } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <Page
      title="Админ-панель"
      description={
        <>
          Вы вошли как <strong>{name}</strong>. Роли:{' '}
          {roles.length > 0 ? roles.join(', ') : '—'}.
        </>
      }
    >
      <List disablePadding>
        <ListItemButton component={RouterLink} to="/admin/specializations">
          <ListItemIcon>
            <CategoryIcon />
          </ListItemIcon>
          <ListItemText primary="Специализации" />
        </ListItemButton>
        <ListItemButton component={RouterLink} to="/admin/doctors">
          <ListItemIcon>
            <MedicalServicesIcon />
          </ListItemIcon>
          <ListItemText primary="Врачи" />
        </ListItemButton>
        <ListItemButton component={RouterLink} to="/admin/patients">
          <ListItemIcon>
            <PeopleIcon />
          </ListItemIcon>
          <ListItemText primary="Пациенты" />
        </ListItemButton>
      </List>
    </Page>
  )
}

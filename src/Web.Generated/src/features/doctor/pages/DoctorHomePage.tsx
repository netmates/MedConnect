import AssignmentIcon from '@mui/icons-material/Assignment'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import { List, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'
import { Page } from '../../../components/Page'

export function DoctorHomePage() {
  const { user, roles } = useAuth()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    'пользователь'

  return (
    <Page
      title="Кабинет врача"
      description={
        <>
          Здравствуйте, <strong>{name}</strong>. Роли: {roles.join(', ') || '—'}.
        </>
      }
    >
      <List disablePadding>
        <ListItemButton component={RouterLink} to="/doctor/schedule">
          <ListItemIcon>
            <CalendarMonthIcon />
          </ListItemIcon>
          <ListItemText primary="Расписание слотов" />
        </ListItemButton>
        <ListItemButton component={RouterLink} to="/doctor/appointments">
          <ListItemIcon>
            <AssignmentIcon />
          </ListItemIcon>
          <ListItemText primary="Приемы" />
        </ListItemButton>
      </List>
    </Page>
  )
}

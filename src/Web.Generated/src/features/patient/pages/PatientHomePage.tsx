import EventNoteIcon from '@mui/icons-material/EventNote'
import MedicalServicesIcon from '@mui/icons-material/MedicalServices'
import PersonIcon from '@mui/icons-material/Person'
import { List, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../../../auth/AuthContext'
import { Page } from '../../../components/Page'
import { sessionLabelFromProfile } from '../../../lib/format'

export function PatientHomePage() {
  const { user } = useAuth()
  const name = sessionLabelFromProfile(user?.profile, 'пользователь')

  return (
    <Page
      title="Кабинет пациента"
      description={
        <>
          Здравствуйте, <strong>{name}</strong>.
        </>
      }
    >
      <List disablePadding>
        <ListItemButton component={RouterLink} to="/patient/profile">
          <ListItemIcon>
            <PersonIcon />
          </ListItemIcon>
          <ListItemText primary="Профиль" />
        </ListItemButton>
        <ListItemButton component={RouterLink} to="/patient/doctors">
          <ListItemIcon>
            <MedicalServicesIcon />
          </ListItemIcon>
          <ListItemText primary="Найти врача и записаться" />
        </ListItemButton>
        <ListItemButton component={RouterLink} to="/patient/appointments">
          <ListItemIcon>
            <EventNoteIcon />
          </ListItemIcon>
          <ListItemText primary="Мои записи" />
        </ListItemButton>
      </List>
    </Page>
  )
}

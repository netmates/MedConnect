import HomeIcon from '@mui/icons-material/Home'
import PersonIcon from '@mui/icons-material/Person'
import MedicalServicesIcon from '@mui/icons-material/MedicalServices'
import EventNoteIcon from '@mui/icons-material/EventNote'
import { AppShell } from './AppShell'

export function PatientLayout() {
  return (
    <AppShell
      brand="MedConnect · Пациент"
      brandTo="/patient"
      nav={[
        { to: '/patient', label: 'Главная', icon: <HomeIcon /> },
        { to: '/patient/profile', label: 'Профиль', icon: <PersonIcon /> },
        {
          to: '/patient/doctors',
          label: 'Врачи',
          icon: <MedicalServicesIcon />,
        },
        {
          to: '/patient/appointments',
          label: 'Записи',
          icon: <EventNoteIcon />,
        },
      ]}
    />
  )
}

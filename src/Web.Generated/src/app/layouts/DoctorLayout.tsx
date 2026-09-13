import HomeIcon from '@mui/icons-material/Home'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import AssignmentIcon from '@mui/icons-material/Assignment'
import { AppShell } from './AppShell'

export function DoctorLayout() {
  return (
    <AppShell
      brand="MedConnect · Врач"
      brandTo="/doctor"
      nav={[
        { to: '/doctor', label: 'Главная', icon: <HomeIcon /> },
        {
          to: '/doctor/schedule',
          label: 'Расписание',
          icon: <CalendarMonthIcon />,
        },
        {
          to: '/doctor/appointments',
          label: 'Приемы',
          icon: <AssignmentIcon />,
        },
      ]}
    />
  )
}

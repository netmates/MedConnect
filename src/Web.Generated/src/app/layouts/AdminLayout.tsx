import HomeIcon from '@mui/icons-material/Home'
import MedicalServicesIcon from '@mui/icons-material/MedicalServices'
import PeopleIcon from '@mui/icons-material/People'
import CategoryIcon from '@mui/icons-material/Category'
import { AppShell } from './AppShell'

export function AdminLayout() {
  return (
    <AppShell
      brand="MedConnect · Админ"
      brandTo="/admin"
      nav={[
        { to: '/admin', label: 'Главная', icon: <HomeIcon /> },
        {
          to: '/admin/specializations',
          label: 'Специализации',
          icon: <CategoryIcon />,
        },
        { to: '/admin/doctors', label: 'Врачи', icon: <MedicalServicesIcon /> },
        { to: '/admin/patients', label: 'Пациенты', icon: <PeopleIcon /> },
      ]}
    />
  )
}

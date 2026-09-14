import BlockIcon from '@mui/icons-material/Block'
import HomeIcon from '@mui/icons-material/Home'
import LoginIcon from '@mui/icons-material/Login'
import SwitchAccountIcon from '@mui/icons-material/SwitchAccount'
import {
  Box,
  Button,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { cabinetRoleLabel, homePathForRoles } from '../auth/roles'

export function AccessDeniedPage() {
  const { roles, logout, user } = useAuth()
  const home = homePathForRoles(roles)
  const roleLabel = cabinetRoleLabel(roles)

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: 2,
        bgcolor: 'background.default',
      }}
    >
      <Paper sx={{ p: 4, maxWidth: 520, width: '100%' }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <BlockIcon color="error" />
            <Typography variant="h1">Нет доступа</Typography>
          </Stack>
          <Typography sx={{
            color: "text.secondary"
          }}>
            {roleLabel
              ? `Этот раздел недоступен для текущей роли: ${roleLabel}.`
              : 'Этот раздел недоступен: у учетной записи нет роли MedConnect.'}
          </Typography>
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
            {user && home !== '/access-denied' && (
              <Button
                component={RouterLink}
                to={home}
                variant="outlined"
                startIcon={<HomeIcon />}
              >
                В мой раздел
              </Button>
            )}
            <Button
              component={RouterLink}
              to="/login"
              variant="outlined"
              startIcon={<LoginIcon />}
            >
              На вход
            </Button>
            <Button
              variant="contained"
              startIcon={<SwitchAccountIcon />}
              onClick={() => void logout()}
            >
              Сменить пользователя
            </Button>
          </Stack>
        </Stack>
      </Paper>
    </Box>
  );
}

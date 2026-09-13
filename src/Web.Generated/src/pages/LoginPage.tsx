import LoginIcon from '@mui/icons-material/Login'
import LocalHospitalIcon from '@mui/icons-material/LocalHospital'
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { login, isLoading, user } = useAuth()

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
      <Paper sx={{ p: 4, maxWidth: 460, width: '100%' }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <LocalHospitalIcon color="primary" />
            <Typography variant="overline" color="primary">
              MedConnect
            </Typography>
          </Stack>
          <Typography variant="h1">Вход</Typography>
          <Typography sx={{
            color: "text.secondary"
          }}>
            Авторизация через Keycloak (Authorization Code + PKCE). После входа
            откроется раздел по роли: admin, doctor или patient.
          </Typography>

          {isLoading ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CircularProgress size={20} />
              <Typography>Проверка сессии…</Typography>
            </Stack>
          ) : user ? (
            <Typography>Вы уже вошли. Перейдите на главную.</Typography>
          ) : (
            <Button
              variant="contained"
              size="large"
              startIcon={<LoginIcon />}
              onClick={() => void login()}
            >
              Войти через Keycloak
            </Button>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}

import LoginIcon from '@mui/icons-material/Login'
import PersonAddIcon from '@mui/icons-material/PersonAdd'
import LocalHospitalIcon from '@mui/icons-material/LocalHospital'
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { PatientSignUpForm } from './PatientSignUpForm'

export function LoginPage() {
  const { login, isLoading, user } = useAuth()
  const [signUp, setSignUp] = useState(false)

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
          <Typography variant="h1">{signUp ? 'Регистрация' : 'Вход'}</Typography>
          <Typography sx={{ color: 'text.secondary' }}>
            {signUp
              ? 'Новая учетная запись создается только для пациента.'
              : 'Войти может администратор, врач или пациент. После входа откроется раздел по роли.'}
          </Typography>

          {isLoading ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CircularProgress size={20} />
              <Typography>Проверка сессии…</Typography>
            </Stack>
          ) : user ? (
            <Typography>Вы уже вошли. Перейдите на главную.</Typography>
          ) : signUp ? (
            <PatientSignUpForm onLogin={() => void login()} onCancel={() => setSignUp(false)} />
          ) : (
            <Stack spacing={1}>
              <Button
                variant="contained"
                size="large"
                startIcon={<LoginIcon />}
                onClick={() => void login()}
              >
                Войти через Keycloak
              </Button>
              <Button
                variant="outlined"
                size="large"
                startIcon={<PersonAddIcon />}
                onClick={() => setSignUp(true)}
              >
                Регистрация пациента
              </Button>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}

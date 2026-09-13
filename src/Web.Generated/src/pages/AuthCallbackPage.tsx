import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import LoginIcon from '@mui/icons-material/Login'
import { userManager } from '../auth/userManager'
import { getRealmRolesFromAccessToken, homePathForRoles } from '../auth/roles'

export function AuthCallbackPage() {
  const navigate = useNavigate()
  const started = useRef(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (started.current) return
    started.current = true

    ;(async () => {
      try {
        const user = await userManager.signinRedirectCallback()
        const roles = getRealmRolesFromAccessToken(user.access_token)
        navigate(homePathForRoles(roles), { replace: true })
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Ошибка входа')
      }
    })()
  }, [navigate])

  if (error) {
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
            <Typography variant="h1">Не удалось войти</Typography>
            <Typography sx={{
              color: "text.secondary"
            }}>{error}</Typography>
            <Button
              variant="contained"
              startIcon={<LoginIcon />}
              onClick={() => navigate('/login')}
            >
              На страницу входа
            </Button>
          </Stack>
        </Paper>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: 2,
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        <CircularProgress size={24} />
        <Typography>Завершение входа…</Typography>
      </Stack>
    </Box>
  )
}

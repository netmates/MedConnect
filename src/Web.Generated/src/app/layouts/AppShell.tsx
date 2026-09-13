import LogoutIcon from '@mui/icons-material/Logout'
import LocalHospitalIcon from '@mui/icons-material/LocalHospital'
import {
  AppBar,
  Box,
  Button,
  Container,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export type NavItem = {
  to: string
  label: string
  icon?: ReactNode
}

type AppShellProps = {
  brand: string
  brandTo: string
  nav: NavItem[]
}

export function AppShell({ brand, brandTo, nav }: AppShellProps) {
  const { user, logout } = useAuth()
  const location = useLocation()
  const name =
    user?.profile?.preferred_username ??
    user?.profile?.name ??
    user?.profile?.sub ??
    'user'

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="sticky" color="primary" elevation={0}>
        <Toolbar sx={{ gap: 1, flexWrap: 'wrap' }}>
          <Stack direction="row" spacing={1} sx={{ mr: 1, alignItems: 'center' }}>
            <LocalHospitalIcon fontSize="small" />
            <Typography
              component={RouterLink}
              to={brandTo}
              variant="subtitle1"
              sx={{ color: 'inherit', textDecoration: 'none', fontWeight: 700 }}
            >
              {brand}
            </Typography>
          </Stack>

          <Stack direction="row" spacing={0.5} sx={{ flex: 1, flexWrap: 'wrap' }}>
            {nav.map((item) => {
              const active =
                item.to === brandTo
                  ? location.pathname === item.to
                  : location.pathname.startsWith(item.to)
              return (
                <Button
                  key={item.to}
                  component={RouterLink}
                  to={item.to}
                  color="inherit"
                  startIcon={item.icon}
                  sx={{
                    opacity: active ? 1 : 0.85,
                    bgcolor: active ? 'rgba(255,255,255,0.14)' : 'transparent',
                  }}
                >
                  {item.label}
                </Button>
              )
            })}
          </Stack>

          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Typography variant="body2" sx={{ opacity: 0.9 }}>
              {name}
            </Typography>
            <Button
              color="inherit"
              startIcon={<LogoutIcon />}
              onClick={() => void logout()}
            >
              Выйти
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 3, flex: 1 }}>
        <Outlet />
      </Container>
    </Box>
  )
}

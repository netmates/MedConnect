import { Alert, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

type PageProps = {
  title: string
  description?: ReactNode
  actions?: ReactNode
  error?: string | null
  ok?: string | null
  children: ReactNode
  wide?: boolean
}

export function Page({
  title,
  description,
  actions,
  error,
  ok,
  children,
}: PageProps) {
  return (
    <Paper sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          sx={{
            justifyContent: 'space-between',
            alignItems: { xs: 'stretch', sm: 'flex-start' },
          }}
        >
          <Stack spacing={0.5}>
            <Typography variant="h1">{title}</Typography>
            {description && (
              <Typography variant="body1" sx={{
                color: "text.secondary"
              }}>
                {description}
              </Typography>
            )}
          </Stack>
          {actions}
        </Stack>

        {error && <Alert severity="error">{error}</Alert>}
        {ok && <Alert severity="success">{ok}</Alert>}

        {children}
      </Stack>
    </Paper>
  );
}

import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1b4f72',
      dark: '#143a54',
      light: '#2e86ab',
    },
    secondary: {
      main: '#0e6655',
    },
    error: {
      main: '#c0392b',
    },
    background: {
      default: '#f4f6f8',
      paper: '#ffffff',
    },
    text: {
      primary: '#1a1d21',
      secondary: '#5c6670',
    },
  },
  typography: {
    fontFamily: '"IBM Plex Sans", "Segoe UI", system-ui, sans-serif',
    h1: { fontSize: '1.75rem', fontWeight: 650 },
    h2: { fontSize: '1.25rem', fontWeight: 600 },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  shape: {
    borderRadius: 10,
  },
  components: {
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          fontWeight: 650,
        },
      },
    },
  },
})

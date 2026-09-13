import BlockIcon from '@mui/icons-material/Block'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import {
  Button,
  Chip,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
} from '@mui/material'
import { useCallback, useEffect, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { patientsApi } from '../../../api/admin/patientsApi'
import { Page } from '../../../components/Page'
import { formatApiError, fullName } from '../../../lib/format'
import type { PatientDto } from '../../../types/patient'

export function PatientsPage() {
  const [items, setItems] = useState<PatientDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await patientsApi.list())
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function toggleActive(patient: PatientDto) {
    const action = patient.isActive ? 'деактивировать' : 'активировать'
    if (!confirm(`Точно ${action} пациента?`)) return
    setBusy(true)
    setError(null)
    try {
      if (patient.isActive) await patientsApi.deactivate(patient.id)
      else await patientsApi.activate(patient.id)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Пациенты"
      description="Список профилей, включая неактивных."
      error={error}
    >
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>ФИО</TableCell>
            <TableCell>Телефон</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((patient) => (
            <TableRow key={patient.id} hover>
              <TableCell>
                {fullName(patient.lastName, patient.firstName, patient.middleName)}
              </TableCell>
              <TableCell>{patient.phone ?? '—'}</TableCell>
              <TableCell>
                <Chip
                  size="small"
                  color={patient.isActive ? 'success' : 'default'}
                  label={patient.isActive ? 'активен' : 'неактивен'}
                />
              </TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                  <Button
                    size="small"
                    component={RouterLink}
                    to={`/admin/patients/${patient.id}`}
                    startIcon={<OpenInNewIcon />}
                  >
                    Открыть
                  </Button>
                  <Button
                    size="small"
                    color={patient.isActive ? 'error' : 'success'}
                    startIcon={patient.isActive ? <BlockIcon /> : <CheckCircleIcon />}
                    disabled={busy}
                    onClick={() => void toggleActive(patient)}
                  >
                    {patient.isActive ? 'Деактивировать' : 'Активировать'}
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          {items.length === 0 && (
            <TableRow>
              <TableCell colSpan={4}>Пока нет пациентов</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}

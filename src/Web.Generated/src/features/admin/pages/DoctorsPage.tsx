import BlockIcon from '@mui/icons-material/Block'
import CancelIcon from '@mui/icons-material/Cancel'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import EditIcon from '@mui/icons-material/Edit'
import KeyIcon from '@mui/icons-material/Key'
import PersonAddIcon from '@mui/icons-material/PersonAdd'
import SaveIcon from '@mui/icons-material/Save'
import {
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  FormGroup,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { doctorsApi } from '../../../api/admin/doctorsApi'
import { specializationsApi } from '../../../api/admin/specializationsApi'
import { Page } from '../../../components/Page'
import { formatApiError, fullName } from '../../../lib/format'
import type { CreateDoctorDto, DoctorDto, UpdateDoctorDto } from '../../../types/doctor'
import type { SpecializationDto } from '../../../types/specialization'

const emptyCreate: CreateDoctorDto = {
  lastName: '',
  firstName: '',
  middleName: '',
  email: '',
  temporaryPassword: '',
  description: '',
  experienceYears: 0,
  specializationIds: [],
}

type Mode = 'create' | 'edit' | 'password' | null

export function DoctorsPage() {
  const [items, setItems] = useState<DoctorDto[]>([])
  const [specs, setSpecs] = useState<SpecializationDto[]>([])
  const [mode, setMode] = useState<Mode>(null)
  const [selected, setSelected] = useState<DoctorDto | null>(null)
  const [createForm, setCreateForm] = useState<CreateDoctorDto>(emptyCreate)
  const [editForm, setEditForm] = useState<UpdateDoctorDto | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      const [doctors, specializations] = await Promise.all([
        doctorsApi.list(),
        specializationsApi.list(),
      ])
      setItems(doctors)
      setSpecs(specializations)
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const specNameToId = useMemo(() => {
    const map = new Map<string, string>()
    for (const s of specs) map.set(s.name, s.id)
    return map
  }, [specs])

  function openCreate() {
    setMode('create')
    setSelected(null)
    setCreateForm({ ...emptyCreate, specializationIds: [] })
  }

  function openEdit(doctor: DoctorDto) {
    const ids = doctor.specializations
      .map((name) => specNameToId.get(name))
      .filter((id): id is string => Boolean(id))

    setSelected(doctor)
    setEditForm({
      lastName: doctor.lastName,
      firstName: doctor.firstName,
      middleName: doctor.middleName ?? '',
      description: doctor.description,
      experienceYears: doctor.experienceYears,
      specializationIds: ids,
    })
    setMode('edit')
  }

  function openPassword(doctor: DoctorDto) {
    setSelected(doctor)
    setNewPassword('')
    setMode('password')
  }

  function toggleSpec(id: string, current: string[], setter: (ids: string[]) => void) {
    setter(current.includes(id) ? current.filter((x) => x !== id) : [...current, id])
  }

  async function submitCreate(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.create({
        ...createForm,
        middleName: createForm.middleName?.trim() || null,
      })
      setMode(null)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function submitEdit(e: FormEvent) {
    e.preventDefault()
    if (!selected || !editForm) return
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.update(selected.id, {
        ...editForm,
        middleName: editForm.middleName?.trim() || null,
      })
      setMode(null)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function submitPassword(e: FormEvent) {
    e.preventDefault()
    if (!selected) return
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.resetPassword(selected.id, { newPassword })
      setMode(null)
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function toggleActive(doctor: DoctorDto) {
    const action = doctor.isActive ? 'деактивировать' : 'активировать'
    if (!confirm(`Точно ${action} врача?`)) return
    setBusy(true)
    setError(null)
    try {
      if (doctor.isActive) await doctorsApi.deactivate(doctor.id)
      else await doctorsApi.activate(doctor.id)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Врачи"
      description="Создание в Keycloak + профиль в БД."
      error={error}
      actions={
        <Button
          variant="contained"
          startIcon={<PersonAddIcon />}
          onClick={openCreate}
          disabled={busy}
        >
          Создать врача
        </Button>
      }
    >
      {mode === 'create' && (
        <Box component="form" onSubmit={(e) => void submitCreate(e)}>
          <Stack spacing={2}>
            <Typography variant="h2">Новый врач</Typography>
            <DoctorFields
              lastName={createForm.lastName}
              firstName={createForm.firstName}
              middleName={createForm.middleName ?? ''}
              description={createForm.description}
              experienceYears={createForm.experienceYears}
              specializationIds={createForm.specializationIds}
              specs={specs}
              onChange={(patch) => setCreateForm((prev) => ({ ...prev, ...patch }))}
              onToggleSpec={(id) =>
                toggleSpec(id, createForm.specializationIds, (specializationIds) =>
                  setCreateForm((prev) => ({ ...prev, specializationIds })),
                )
              }
            />
            <TextField
              label="Email"
              type="email"
              required
              value={createForm.email}
              onChange={(e) => setCreateForm((p) => ({ ...p, email: e.target.value }))}
            />
            <TextField
              label="Временный пароль"
              type="password"
              required
              value={createForm.temporaryPassword}
              onChange={(e) =>
                setCreateForm((p) => ({ ...p, temporaryPassword: e.target.value }))
              }
            />
            <Stack direction="row" spacing={1}>
              <Button type="submit" variant="contained" disabled={busy} startIcon={<SaveIcon />}>
                Создать
              </Button>
              <Button startIcon={<CancelIcon />} onClick={() => setMode(null)}>
                Отмена
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}

      {mode === 'edit' && editForm && selected && (
        <Box component="form" onSubmit={(e) => void submitEdit(e)}>
          <Stack spacing={2}>
            <Typography variant="h2">
              Редактирование:{' '}
              {fullName(selected.lastName, selected.firstName, selected.middleName)}
            </Typography>
            <DoctorFields
              lastName={editForm.lastName}
              firstName={editForm.firstName}
              middleName={editForm.middleName ?? ''}
              description={editForm.description}
              experienceYears={editForm.experienceYears}
              specializationIds={editForm.specializationIds}
              specs={specs}
              onChange={(patch) => setEditForm((prev) => (prev ? { ...prev, ...patch } : prev))}
              onToggleSpec={(id) =>
                toggleSpec(id, editForm.specializationIds, (specializationIds) =>
                  setEditForm((prev) => (prev ? { ...prev, specializationIds } : prev)),
                )
              }
            />
            <Stack direction="row" spacing={1}>
              <Button type="submit" variant="contained" disabled={busy} startIcon={<SaveIcon />}>
                Сохранить
              </Button>
              <Button startIcon={<CancelIcon />} onClick={() => setMode(null)}>
                Отмена
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}

      {mode === 'password' && selected && (
        <Box component="form" onSubmit={(e) => void submitPassword(e)}>
          <Stack spacing={2}>
            <Typography variant="h2">
              Сброс пароля:{' '}
              {fullName(selected.lastName, selected.firstName, selected.middleName)}
            </Typography>
            <TextField
              label="Новый пароль"
              type="password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <Stack direction="row" spacing={1}>
              <Button type="submit" variant="contained" disabled={busy} startIcon={<KeyIcon />}>
                Сбросить
              </Button>
              <Button startIcon={<CancelIcon />} onClick={() => setMode(null)}>
                Отмена
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>ФИО</TableCell>
            <TableCell>Стаж</TableCell>
            <TableCell>Специализации</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((doctor) => (
            <TableRow key={doctor.id} hover>
              <TableCell>
                {fullName(doctor.lastName, doctor.firstName, doctor.middleName)}
              </TableCell>
              <TableCell>{doctor.experienceYears}</TableCell>
              <TableCell>{doctor.specializations.join(', ') || '—'}</TableCell>
              <TableCell>
                <Chip
                  size="small"
                  color={doctor.isActive ? 'success' : 'default'}
                  label={doctor.isActive ? 'активен' : 'неактивен'}
                />
              </TableCell>
              <TableCell align="right">
                <Stack
                  direction="row"
                  spacing={0.5}
                  useFlexGap
                  sx={{ justifyContent: 'flex-end', flexWrap: 'wrap' }}
                >
                  <Button
                    size="small"
                    startIcon={<EditIcon />}
                    disabled={busy}
                    onClick={() => openEdit(doctor)}
                  >
                    Изменить
                  </Button>
                  <Button
                    size="small"
                    startIcon={<KeyIcon />}
                    disabled={busy}
                    onClick={() => openPassword(doctor)}
                  >
                    Пароль
                  </Button>
                  <Button
                    size="small"
                    color={doctor.isActive ? 'error' : 'success'}
                    startIcon={doctor.isActive ? <BlockIcon /> : <CheckCircleIcon />}
                    disabled={busy}
                    onClick={() => void toggleActive(doctor)}
                  >
                    {doctor.isActive ? 'Деактивировать' : 'Активировать'}
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          {items.length === 0 && (
            <TableRow>
              <TableCell colSpan={5}>Пока нет врачей</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}

type DoctorFieldsProps = {
  lastName: string
  firstName: string
  middleName: string
  description: string
  experienceYears: number
  specializationIds: string[]
  specs: SpecializationDto[]
  onChange: (patch: Partial<UpdateDoctorDto>) => void
  onToggleSpec: (id: string) => void
}

function DoctorFields({
  lastName,
  firstName,
  middleName,
  description,
  experienceYears,
  specializationIds,
  specs,
  onChange,
  onToggleSpec,
}: DoctorFieldsProps) {
  return (
    <>
      <TextField
        label="Фамилия"
        required
        value={lastName}
        onChange={(e) => onChange({ lastName: e.target.value })}
      />
      <TextField
        label="Имя"
        required
        value={firstName}
        onChange={(e) => onChange({ firstName: e.target.value })}
      />
      <TextField
        label="Отчество"
        value={middleName}
        onChange={(e) => onChange({ middleName: e.target.value })}
      />
      <TextField
        label="Описание"
        required
        multiline
        minRows={3}
        value={description}
        onChange={(e) => onChange({ description: e.target.value })}
      />
      <TextField
        label="Стаж (лет)"
        type="number"
        required
        slotProps={{ htmlInput: { min: 0 } }}
        value={experienceYears}
        onChange={(e) => onChange({ experienceYears: Number(e.target.value) })}
      />
      <Box>
        <Typography variant="subtitle2" gutterBottom>
          Специализации
        </Typography>
        <FormGroup>
          {specs.map((spec) => (
            <FormControlLabel
              key={spec.id}
              control={
                <Checkbox
                  checked={specializationIds.includes(spec.id)}
                  onChange={() => onToggleSpec(spec.id)}
                />
              }
              label={spec.name}
            />
          ))}
        </FormGroup>
        {specs.length === 0 && (
          <Typography color="text.secondary">Сначала создайте специализации.</Typography>
        )}
      </Box>
    </>
  )
}

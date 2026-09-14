import SendIcon from '@mui/icons-material/Send'
import {
  Box,
  Button,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { appointmentsApi } from '../../api/appointmentsApi'
import { chatsApi } from '../../api/chatsApi'
import { useAuth } from '../../auth/AuthContext'
import { Page } from '../../components/Page'
import { formatApiError, formatDateTime } from '../../lib/format'
import type { AppointmentDto } from '../../types/appointment'
import type { CreateChatResponse, MessageResponse } from '../../types/chat'

type ChatPageProps = {
  backTo: string
  backLabel: string
}

export function ChatPage({ backTo, backLabel }: ChatPageProps) {
  const { appointmentId } = useParams<{ appointmentId: string }>()
  const { user } = useAuth()
  const mySub = user?.profile?.sub ?? ''
  const [appointment, setAppointment] = useState<AppointmentDto | null>(null)
  const [chat, setChat] = useState<CreateChatResponse | null>(null)
  const [messages, setMessages] = useState<MessageResponse[]>([])
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const bottomRef = useRef<HTMLDivElement | null>(null)

  const loadMessages = useCallback(async (chatId: string) => {
    setMessages(await chatsApi.messages(chatId))
  }, [])

  useEffect(() => {
    if (!appointmentId) return
    let cancelled = false

    ;(async () => {
      setError(null)
      try {
        const appt = await appointmentsApi.get(appointmentId)
        if (cancelled) return
        setAppointment(appt)
        const ensured = await chatsApi.ensure({ appointmentId })
        if (cancelled) return
        setChat(ensured)
        await loadMessages(ensured.id)
      } catch (e) {
        if (!cancelled) setError(formatApiError(e))
      }
    })()

    return () => {
      cancelled = true
    }
  }, [appointmentId, loadMessages])

  useEffect(() => {
    if (!chat) return
    const timer = window.setInterval(() => {
      void loadMessages(chat.id).catch(() => undefined)
    }, 4000)
    return () => window.clearInterval(timer)
  }, [chat, loadMessages])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function onSend(e: FormEvent) {
    e.preventDefault()
    if (!chat || !text.trim()) return
    setBusy(true)
    setError(null)
    try {
      const sent = await chatsApi.send(chat.id, { text: text.trim() })
      setMessages((prev) => [...prev, sent])
      setText('')
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Чат по приему"
      description={
        appointment
          ? `${appointment.doctorFullName} · ${appointment.patientFullName} · ${formatDateTime(appointment.startTime)}`
          : undefined
      }
      error={error}
      actions={
        <Button component={RouterLink} to={backTo}>
          {backLabel}
        </Button>
      }
    >
      <Paper
        variant="outlined"
        sx={{
          p: 2,
          minHeight: 320,
          maxHeight: 480,
          overflowY: 'auto',
          bgcolor: 'grey.50',
        }}
      >
        <Stack spacing={1.25}>
          {messages.map((msg) => {
            const mine = msg.senderId === mySub
            return (
              <Box
                key={msg.id}
                sx={{
                  alignSelf: mine ? 'flex-end' : 'flex-start',
                  maxWidth: '75%',
                  px: 1.5,
                  py: 1,
                  borderRadius: 2,
                  bgcolor: mine ? 'primary.main' : 'background.paper',
                  color: mine ? 'primary.contrastText' : 'text.primary',
                  border: mine ? 'none' : '1px solid',
                  borderColor: 'divider',
                }}
              >
                <Typography
                  variant="caption"
                  sx={{ opacity: 0.8, display: 'block', mb: 0.5 }}
                >
                  {msg.senderRole} · {formatDateTime(msg.createdAt)}
                </Typography>
                <Typography variant="body2">{msg.text}</Typography>
              </Box>
            )
          })}
          {messages.length === 0 && (
            <Typography color="text.secondary">Сообщений пока нет</Typography>
          )}
          <div ref={bottomRef} />
        </Stack>
      </Paper>

      <Stack
        component="form"
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        onSubmit={(e) => void onSend(e)}
      >
        <TextField
          fullWidth
          size="small"
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Сообщение"
          disabled={!chat || busy}
        />
        <Button
          type="submit"
          variant="contained"
          startIcon={<SendIcon />}
          disabled={!chat || busy || !text.trim()}
        >
          Отправить
        </Button>
      </Stack>
    </Page>
  )
}

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
import { appendUniqueMessage, useChatHub } from './useChatHub'

type ChatPageProps = {
  backTo: string
  backLabel: string
  viewer: 'doctor' | 'patient'
}

export function ChatPage({ backTo, backLabel, viewer }: ChatPageProps) {
  const { appointmentId } = useParams<{ appointmentId: string }>()
  const { user, getAccessToken } = useAuth()
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

  const onLiveMessage = useCallback((message: MessageResponse) => {
    setMessages((prev) => appendUniqueMessage(prev, message))
  }, [])

  const onHubReconnected = useCallback(() => {
    if (!chat) return
    void loadMessages(chat.id).catch((e) => setError(formatApiError(e)))
  }, [chat, loadMessages])

  useChatHub({
    chatId: chat?.id,
    getAccessToken,
    onMessage: onLiveMessage,
    onReconnected: onHubReconnected,
    onError: setError,
  })

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
      setMessages((prev) => appendUniqueMessage(prev, sent))
      setText('')
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  const doctorName = chat?.doctorName || appointment?.doctorFullName || ''
  const patientName = chat?.patientName || appointment?.patientFullName || ''
  const myName = viewer === 'doctor' ? doctorName : patientName
  const otherName = viewer === 'doctor' ? patientName : doctorName

  return (
    <Page
      title="Чат по приему"
      error={error}
      actions={
        <Button component={RouterLink} to={backTo}>
          {backLabel}
        </Button>
      }
    >
      {appointment && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'minmax(0, 1fr) auto minmax(0, 1fr)',
            columnGap: 2,
            alignItems: 'center',
            color: 'text.secondary',
          }}
        >
          <Typography variant="body1" noWrap sx={{ textAlign: 'left' }}>
            {otherName}
          </Typography>
          <Typography variant="body1" sx={{ textAlign: 'center', whiteSpace: 'nowrap' }}>
            {formatDateTime(appointment.startTime)}
          </Typography>
          <Typography variant="body1" noWrap sx={{ textAlign: 'right' }}>
            {myName}
          </Typography>
        </Box>
      )}
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

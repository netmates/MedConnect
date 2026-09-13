import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { appointmentsApi } from '../../api/appointmentsApi'
import { chatsApi } from '../../api/chatsApi'
import { useAuth } from '../../auth/AuthContext'
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
    <section className="panel panel-wide">
      <p>
        <Link to={backTo}>{backLabel}</Link>
      </p>
      <h1>Чат по приему</h1>
      {appointment && (
        <p className="lead">
          {appointment.doctorFullName} · {appointment.patientFullName} ·{' '}
          {formatDateTime(appointment.startTime)}
        </p>
      )}

      {error && <p className="error-banner">{error}</p>}

      <div className="chat-box">
        {messages.map((msg) => {
          const mine = msg.senderId === mySub
          return (
            <div key={msg.id} className={`chat-bubble ${mine ? 'mine' : 'theirs'}`}>
              <div className="chat-meta">
                {msg.senderRole} · {formatDateTime(msg.createdAt)}
              </div>
              <div>{msg.text}</div>
            </div>
          )
        })}
        {messages.length === 0 && <p className="page-status">Сообщений пока нет</p>}
        <div ref={bottomRef} />
      </div>

      <form className="form-row chat-form" onSubmit={(e) => void onSend(e)}>
        <input
          className="input"
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Сообщение"
          disabled={!chat || busy}
        />
        <button className="btn btn-primary" type="submit" disabled={!chat || busy || !text.trim()}>
          Отправить
        </button>
      </form>
    </section>
  )
}

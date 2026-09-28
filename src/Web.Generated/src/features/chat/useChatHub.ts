import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { config } from '../../config'
import type { MessageResponse } from '../../types/chat'

const hubUrl = `${config.apiUrl}/api/chats/hub`

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string {
  const value = raw[camel] ?? raw[pascal]
  if (value == null) return ''
  return String(value)
}

export function toMessageResponse(raw: unknown): MessageResponse | null {
  if (!raw || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const id = readString(o, 'id', 'Id')
  const chatId = readString(o, 'chatId', 'ChatId')
  const text = readString(o, 'text', 'Text')
  if (!id || !chatId || !text) return null

  return {
    id,
    chatId,
    senderId: readString(o, 'senderId', 'SenderId'),
    senderRole: readString(o, 'senderRole', 'SenderRole'),
    text,
    createdAt: readString(o, 'createdAt', 'CreatedAt'),
  }
}

export function appendUniqueMessage(
  prev: MessageResponse[],
  message: MessageResponse,
): MessageResponse[] {
  if (prev.some((item) => item.id === message.id)) return prev
  return [...prev, message]
}

type UseChatHubOptions = {
  chatId: string | undefined
  getAccessToken: () => Promise<string | null>
  onMessage: (message: MessageResponse) => void
  onReconnected?: () => void
  onError: (message: string) => void
}

export function useChatHub({
  chatId,
  getAccessToken,
  onMessage,
  onReconnected,
  onError,
}: UseChatHubOptions): void {
  const onMessageRef = useRef(onMessage)
  const onReconnectedRef = useRef(onReconnected)
  const onErrorRef = useRef(onError)
  const getAccessTokenRef = useRef(getAccessToken)

  onMessageRef.current = onMessage
  onReconnectedRef.current = onReconnected
  onErrorRef.current = onError
  getAccessTokenRef.current = getAccessToken

  useEffect(() => {
    if (!chatId) return

    let stopped = false
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () => (await getAccessTokenRef.current()) ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('ReceiveMessage', (payload: unknown) => {
      const message = toMessageResponse(payload)
      if (message && message.chatId === chatId) onMessageRef.current(message)
    })

    async function join(): Promise<void> {
      await connection.invoke('JoinChat', chatId)
    }

    connection.onreconnected(() => {
      void join()
        .then(() => onReconnectedRef.current?.())
        .catch(() => {
          onErrorRef.current('Не удалось переподключить чат.')
        })
    })

    ;(async () => {
      try {
        await connection.start()
        if (stopped) return
        await join()
      } catch {
        if (!stopped) onErrorRef.current('Не удалось подключить живой чат.')
      }
    })()

    return () => {
      stopped = true
      connection.off('ReceiveMessage')
      void (async () => {
        try {
          if (connection.state === HubConnectionState.Connected) {
            await connection.invoke('LeaveChat', chatId)
          }
        } catch {
          // закрываем соединение в любом случае
        } finally {
          await connection.stop()
        }
      })()
    }
  }, [chatId])
}

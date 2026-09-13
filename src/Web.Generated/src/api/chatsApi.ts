import { apiFetch } from './http'
import type {
  CreateChatRequest,
  CreateChatResponse,
  MessageResponse,
  SendMessageRequest,
} from '../types/chat'

export const chatsApi = {
  ensure: (dto: CreateChatRequest) =>
    apiFetch<CreateChatResponse>('/api/chats/', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
  messages: (chatId: string) =>
    apiFetch<MessageResponse[]>(`/api/chats/${chatId}/messages`),
  send: (chatId: string, dto: SendMessageRequest) =>
    apiFetch<MessageResponse>(`/api/chats/${chatId}/messages`, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
}

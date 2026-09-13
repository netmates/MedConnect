export type CreateChatRequest = {
  appointmentId: string
}

export type CreateChatResponse = {
  id: string
  appointmentId: string
  patientId: string
  doctorId: string
  patientName: string
  doctorName: string
  createdAt: string
}

export type SendMessageRequest = {
  text: string
}

export type MessageResponse = {
  id: string
  chatId: string
  senderId: string
  senderRole: string
  text: string
  createdAt: string
}

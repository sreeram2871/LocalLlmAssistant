import { Injectable } from '@angular/core';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}


export interface LlmGenerationOptions {
  model?: string;
  temperature?: number;
  topK?: number;
  topP?: number;
  maxTokens?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {

private readonly apiUrl =
  'http://localhost:8080/api/Chat';


    private readonly chatsApiUrl =
    'http://localhost:8080/api/Chats';

    
async streamMessage(
  messages: ChatMessage[],
  onChunk: (chunk: string) => void,
  signal?: AbortSignal,
  options?: LlmGenerationOptions,
  chatId?: string | null
): Promise<string | null> {

  const response = await fetch(
    `${this.apiUrl}/stream`,
    {
      method: 'POST',

      headers: {
        'Content-Type': 'application/json'
      },

      body: JSON.stringify({
        chatId,
        messages,
        options
      }),

      signal
    }
  );

  if (!response.ok) {
    throw new Error(
      `Request failed: ${response.status}`
    );
  }

  if (!response.body) {
    throw new Error(
      'Response body is empty.'
    );
  }

  // -----------------------------------------
  // Get Chat ID returned by backend
  // -----------------------------------------

  const responseChatId =
    response.headers.get(
      'X-Chat-Id'
    );

  const reader =
    response.body.getReader();

  const decoder =
    new TextDecoder();

  try {

    while (true) {

      const {
        value,
        done
      } = await reader.read();

      if (done) {
        break;
      }

      const chunk =
        decoder.decode(
          value,
          {
            stream: true
          }
        );

      if (chunk) {
        onChunk(chunk);
      }

    }

  }
  finally {

    reader.releaseLock();

  }

  return responseChatId;
}
}
import {
  Component,
  signal,
  ElementRef,
  ViewChild,
  ViewEncapsulation,
  AfterViewChecked,
  Inject,
  PLATFORM_ID
} from '@angular/core';

import { isPlatformBrowser } from '@angular/common';

import { FormsModule } from '@angular/forms';

import { marked } from 'marked';

import {
  ChatService,
  ChatMessage,
  LlmGenerationOptions
} from '../services/chat.service';


// =====================================================
// LLM METRICS
// =====================================================

interface LlmMetrics {
  model: string;
  promptTokens: number;
  outputTokens: number;
  totalSeconds: number;
  loadSeconds: number;
  generationSeconds: number;
  tokensPerSecond: number;
}


@Component({
  selector: 'app-chat',

  imports: [
    FormsModule
  ],

  templateUrl: './chat.html',

  styleUrl: './chat.css',

  encapsulation: ViewEncapsulation.None
})
export class Chat implements AfterViewChecked {


  // =====================================================
  // CHAT ID
  // =====================================================

  // null = new conversation
  // GUID = existing conversation

  chatId: string | null = null;


  // =====================================================
  // USER INPUT
  // =====================================================

  message = '';


  // =====================================================
  // CHAT MESSAGES
  // =====================================================

  messages =
    signal<ChatMessage[]>([]);


  // =====================================================
  // LOADING STATE
  // =====================================================

  isLoading =
    signal(false);


  // =====================================================
  // ABORT CONTROLLER
  // =====================================================

  private abortController?: AbortController;


  // =====================================================
  // MESSAGE CONTAINER
  // =====================================================

  @ViewChild('messagesContainer')
  messagesContainer!: ElementRef<HTMLDivElement>;


  // =====================================================
  // METRICS
  // =====================================================

  metrics =
    signal<LlmMetrics | null>(null);

  showMetrics =
    signal<boolean>(false);


  // =====================================================
  // SETTINGS
  // =====================================================

  model = 'llama3.2';

  temperature = 0.7;

  topK = 40;

  topP = 0.9;

  maxTokens = 1000;

  showSettings =
    signal<boolean>(false);


  // =====================================================
  // CONSTRUCTOR
  // =====================================================

constructor(
  private chatService: ChatService,
  @Inject(PLATFORM_ID)
  private platformId: object
) {
}

  // =====================================================
  // TOGGLE METRICS
  // =====================================================

  toggleMetrics(): void {

    this.showMetrics.update(
      value => !value
    );

    // Close settings
    this.showSettings.set(false);
  }


  // =====================================================
  // TOGGLE SETTINGS
  // =====================================================

  toggleSettings(): void {

    this.showSettings.update(
      value => !value
    );

    // Close metrics
    this.showMetrics.set(false);
  }


  // =====================================================
  // CODE BLOCK / COPY BUTTON
  // =====================================================

 ngAfterViewChecked(): void {

  // ---------------------------------------------
  // Don't execute DOM code during SSR/prerender
  // ---------------------------------------------

  if (!isPlatformBrowser(this.platformId)) {
    return;
  }


  const container =
    this.messagesContainer?.nativeElement;

  if (!container) {
    return;
  }


  const codeBlocks =
    Array.from(
      container.querySelectorAll('pre')
    );


  codeBlocks.forEach(pre => {

    // -------------------------------------------
    // Already processed?
    // -------------------------------------------

    if (
      pre.parentElement?.classList.contains(
        'code-block'
      )
    ) {
      return;
    }


    // -------------------------------------------
    // Find code element
    // -------------------------------------------

    const codeElement =
      pre.querySelector('code');

    if (!codeElement) {
      return;
    }


    // -------------------------------------------
    // Create wrapper
    // -------------------------------------------

    const wrapper =
      document.createElement('div');

    wrapper.className =
      'code-block';


    // -------------------------------------------
    // Create header
    // -------------------------------------------

    const header =
      document.createElement('div');

    header.className =
      'code-header';


    // -------------------------------------------
    // Language
    // -------------------------------------------

    const language =
      document.createElement('span');

    language.className =
      'code-language';


    const languageClass =
      Array.from(
        codeElement.classList
      ).find(
        className =>
          className.startsWith(
            'language-'
          )
      );


    language.textContent =
      languageClass
        ? languageClass.replace(
            'language-',
            ''
          )
        : 'code';


    // -------------------------------------------
    // Copy button
    // -------------------------------------------

    const copyButton =
      document.createElement('button');

    copyButton.type =
      'button';

    copyButton.className =
      'copy-code-button';

    copyButton.textContent =
      'Copy';


    copyButton.addEventListener(
      'click',
      async () => {

        const code =
          codeElement.textContent ?? '';

        try {

          await navigator.clipboard.writeText(
            code
          );

          copyButton.textContent =
            'Copied!';

          setTimeout(() => {

            copyButton.textContent =
              'Copy';

          }, 1500);

        }
        catch (error) {

          console.error(
            'Copy failed:',
            error
          );

          copyButton.textContent =
            'Failed';

          setTimeout(() => {

            copyButton.textContent =
              'Copy';

          }, 1500);

        }

      }
    );


    // -------------------------------------------
    // Header
    // -------------------------------------------

    header.appendChild(
      language
    );

    header.appendChild(
      copyButton
    );


    // -------------------------------------------
    // Insert wrapper
    // -------------------------------------------

    pre.parentNode?.insertBefore(
      wrapper,
      pre
    );

    wrapper.appendChild(
      header
    );

    wrapper.appendChild(
      pre
    );

  });
}
  // =====================================================
  // MARKDOWN
  // =====================================================

  renderMarkdown(
    content: string
  ): string {

    if (!content) {
      return '';
    }

    return marked.parse(
      content
    ) as string;
  }


  // =====================================================
  // NEW CHAT
  // =====================================================

  newChat(): void {

    // Don't clear while generating
    if (this.isLoading()) {
      return;
    }


    // Clear frontend messages
    this.messages.set([]);


    // IMPORTANT:
    // Clear chatId so next question
    // creates a new database chat.
    this.chatId = null;


    // Clear metrics
    this.metrics.set(null);


    // Close popups
    this.showMetrics.set(false);

    this.showSettings.set(false);


    // Clear input
    this.message = '';
  }


  // =====================================================
  // SCROLL TO BOTTOM
  // =====================================================

  private scrollToBottom(): void {

    setTimeout(() => {

      const element =
        this.messagesContainer?.nativeElement;

      if (element) {

        element.scrollTop =
          element.scrollHeight;

      }

    });

  }


  // =====================================================
  // ASK QUESTION
  // =====================================================

  async askQuestion(): Promise<void> {

    // ---------------------------------------------------
    // Get question
    // ---------------------------------------------------

    const question =
      this.message.trim();


    // ---------------------------------------------------
    // Empty question
    // ---------------------------------------------------

    if (!question) {
      return;
    }


    // ---------------------------------------------------
    // Prevent multiple requests
    // ---------------------------------------------------

    if (this.isLoading()) {
      return;
    }


    // ---------------------------------------------------
    // Clear input
    // ---------------------------------------------------

    this.message = '';


    // ---------------------------------------------------
    // Start loading
    // ---------------------------------------------------

    this.isLoading.set(true);


    // Clear previous metrics
    this.metrics.set(null);


    // Close metrics/settings while generating
    this.showMetrics.set(false);

    this.showSettings.set(false);


    // ---------------------------------------------------
    // Abort controller
    // ---------------------------------------------------

    this.abortController =
      new AbortController();


    // ===================================================
    // ADD USER MESSAGE
    // ===================================================

    this.messages.update(
      current => [

        ...current,

        {
          role: 'user',
          content: question
        }

      ]
    );


    // ===================================================
    // ADD EMPTY ASSISTANT MESSAGE
    // ===================================================

    this.messages.update(
      current => [

        ...current,

        {
          role: 'assistant',
          content: ''
        }

      ]
    );


    // Scroll
    this.scrollToBottom();


    try {

      // =================================================
      // CONVERSATION
      // =================================================

      const conversation =
        this.messages()
          .filter(
            message =>
              message.content.trim() !== ''
          );


      console.log(
        'Sending conversation:',
        conversation
      );


      // =================================================
      // SETTINGS
      // =================================================

      const options:
        LlmGenerationOptions = {

        model:
          this.model,

        temperature:
          Number(this.temperature),

        topK:
          Number(this.topK),

        topP:
          Number(this.topP),

        maxTokens:
          Number(this.maxTokens)

      };


      console.log(
        'LLM OPTIONS:',
        options
      );


      // =================================================
      // STREAM
      // =================================================

      const responseChatId =
        await this.chatService.streamMessage(

          // ----------------------------------------------
          // Conversation
          // ----------------------------------------------

          conversation,


          // ----------------------------------------------
          // Chunk callback
          // ----------------------------------------------

          (chunk: string) => {

            console.log(
              'CHUNK RECEIVED:',
              chunk
            );


            // =================================================
            // METRICS
            // =================================================

            if (
              chunk.startsWith(
                '[[LLM_METRICS]]'
              )
            ) {

              const json =
                chunk.replace(
                  '[[LLM_METRICS]]',
                  ''
                );


              try {

                const metrics =
                  JSON.parse(
                    json
                  ) as LlmMetrics;


                this.metrics.set(
                  metrics
                );


                console.log(
                  'LLM METRICS:',
                  metrics
                );

              }
              catch (error) {

                console.error(
                  'Failed to parse LLM metrics:',
                  error
                );

              }


              return;
            }


            // =================================================
            // ERROR
            // =================================================

            if (
              chunk.startsWith(
                '[[ERROR]]'
              )
            ) {

              const errorMessage =
                chunk
                  .replace(
                    '[[ERROR]]',
                    ''
                  )
                  .trim();


              this.messages.update(
                current => {

                  const updated =
                    [...current];


                  const lastMessage =
                    updated[
                      updated.length - 1
                    ];


                  if (
                    lastMessage &&
                    lastMessage.role ===
                      'assistant'
                  ) {

                    updated[
                      updated.length - 1
                    ] = {

                      ...lastMessage,

                      content:
                        `⚠️ ${errorMessage}`

                    };

                  }


                  return updated;

                }
              );


              this.scrollToBottom();

              return;
            }


            // =================================================
            // NORMAL LLM CHUNK
            // =================================================

            this.messages.update(
              current => {

                const updated =
                  [...current];


                const lastMessage =
                  updated[
                    updated.length - 1
                  ];


                if (
                  lastMessage &&
                  lastMessage.role ===
                    'assistant'
                ) {

                  updated[
                    updated.length - 1
                  ] = {

                    ...lastMessage,

                    content:
                      lastMessage.content +
                      chunk

                  };

                }


                return updated;

              }
            );


            // Scroll
            this.scrollToBottom();

          },


          // ----------------------------------------------
          // Abort signal
          // ----------------------------------------------

          this.abortController.signal,


          // ----------------------------------------------
          // Generation options
          // ----------------------------------------------

          options,


          // ----------------------------------------------
          // Existing chat ID
          // ----------------------------------------------

          this.chatId

        );


      // =================================================
      // SAVE CHAT ID
      // =================================================

      if (responseChatId) {

        this.chatId =
          responseChatId;


        console.log(
          'CHAT ID:',
          this.chatId
        );

      }


      console.log(
        'STREAM COMPLETED'
      );

    }


    // =====================================================
    // ERROR
    // =====================================================

    catch (error) {

      // ---------------------------------------------------
      // User stopped generation
      // ---------------------------------------------------

      if (
        error instanceof DOMException &&
        error.name ===
          'AbortError'
      ) {

        console.log(
          'Generation stopped by user.'
        );

        return;
      }


      // ---------------------------------------------------
      // Actual error
      // ---------------------------------------------------

      console.error(
        'Streaming error:',
        error
      );


      this.messages.update(
        current => {

          const updated =
            [...current];


          const lastMessage =
            updated[
              updated.length - 1
            ];


          if (
            lastMessage &&
            lastMessage.role ===
              'assistant'
          ) {

            updated[
              updated.length - 1
            ] = {

              ...lastMessage,

              content:
                error instanceof Error
                  ? `⚠️ ${error.message}`
                  : '⚠️ An unexpected error occurred.'

            };

          }


          return updated;

        }
      );

    }


    // =====================================================
    // FINALLY
    // =====================================================

    finally {

      // Stop loading
      this.isLoading.set(false);


      // Remove controller
      this.abortController =
        undefined;


      console.log(
        'STREAM FINISHED'
      );

    }

  }


  // =====================================================
  // STOP GENERATION
  // =====================================================

  stopGeneration(): void {

    console.log(
      'Stopping generation...'
    );


    if (
      this.abortController
    ) {

      // Cancel request
      this.abortController.abort();


      // Clear controller
      this.abortController =
        undefined;


      // Update UI
      this.isLoading.set(false);


      console.log(
        'Generation stopped by user.'
      );

    }

  }

}
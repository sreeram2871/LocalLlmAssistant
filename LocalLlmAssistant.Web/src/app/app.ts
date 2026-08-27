import { Component, signal } from '@angular/core';

import { Chat } from './chat/chat';

@Component({
  imports: [ Chat],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('LocalLlmAssistant.Web');
}
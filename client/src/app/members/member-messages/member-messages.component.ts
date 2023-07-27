import { ChangeDetectionStrategy } from '@angular/core';
import { Component, Input, ViewChild  } from '@angular/core';
import { NgForm } from '@angular/forms';

import { Message } from 'src/app/_models/message';
import { MessageService } from 'src/app/_services/message.service';

@Component({
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MemberMessagesComponent {
  @ViewChild('form') form: NgForm;
  @Input() messages: Message[];
  @Input() userName: string;
  content: string;

  constructor(public messageService: MessageService) {}

  sendMessage() {
    this.messageService.sendMessage(this.userName, this.content ).then(() => {
      this.form.reset();
    });
  }
}

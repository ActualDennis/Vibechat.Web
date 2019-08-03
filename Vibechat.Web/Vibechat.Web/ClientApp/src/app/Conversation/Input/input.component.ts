import { Component, Input, Output, EventEmitter, ViewChild } from "@angular/core";
import { ConversationTemplate } from "../../Data/ConversationTemplate";
import { MatFormField } from "@angular/material";
import { UserInfo } from "../../Data/UserInfo";
import { ChatsService } from "../../Services/ChatsService";

@Component({
  selector: 'input-view',
  templateUrl: './input.component.html',
  styleUrls: ['./input.component.css']
})
export class InputComponent {

  @Output() public OnSendMessage = new EventEmitter<string>();

  @Output() public OnViewUserInfo = new EventEmitter<UserInfo>();

  @Input() public User: UserInfo;

  @ViewChild(MatFormField, { static: false }) inputfield: MatFormField;

  constructor(private chats: ChatsService) {

  }

  @Input() public Conversation: ConversationTemplate;

  public uploadProgress: number = 0;

  public uploading: boolean = false;

  public isTypingSent: boolean = false;

  public TypingDelay: number = 1000;

  public SendMessage() {

    if (this.inputfield._control.value == null || this.inputfield._control.value == '') {
      return;
    }

    this.OnSendMessage.emit(this.inputfield._control.value);

    this.inputfield._control.value = '';
  }

  public ProgressCallback(value: number) {
    this.uploadProgress = value;
  }

  public async UploadFile(event: Event) {
    try {
      this.uploading = true;
      await this.chats.UploadFile((<HTMLInputElement>event.target).files[0], this.ProgressCallback.bind(this), this.Conversation);
    } finally {
      this.ResetInput(<HTMLInputElement>event.target);
      this.uploading = false;
    }
  }

  public SetTyping() {
    if (this.isTypingSent) {
      return;
    }

    this.chats.SetTyping(this.Conversation.conversationID);
    setTimeout(() => this.isTypingSent = false, this.TypingDelay);
  }

  public ViewUserInfo() {
    this.OnViewUserInfo.emit(this.User);
  }

  public ResetInput(input: HTMLInputElement) {
    input.value = '';

    if (!/safari/i.test(navigator.userAgent)) {
      input.type = '';
      input.type = 'file';
    }
  }

  public async UploadImages(event: Event) {
    try {
      this.uploading = true;
      await this.chats.UploadImages((<HTMLInputElement>event.target).files, this.ProgressCallback.bind(this), this.Conversation);
    } finally {
      this.uploading = false;
      this.ResetInput(<HTMLInputElement>event.target);
    }
  }
}

import { Component, Inject, EventEmitter } from "@angular/core";
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from "@angular/material";
import { ChatComponent } from "../Chat/chat.component";
import { ConversationTemplate } from "../Data/ConversationTemplate";
import { UserInfo } from "../Data/UserInfo";
import { ConversationsFormatter } from "../Formatters/ConversationsFormatter";
import { ChangeNameDialogComponent } from "./ChangeNameDialog";
import { FindUsersDialogComponent } from "./FindUsersDialog";
import { ConversationsService } from "../Services/ConversationsService";
import { ViewAttachmentsDialogComponent } from "./ViewAttachmentsDialog";

export interface  GroupInfoData {
  Conversation: ConversationTemplate;
  user: UserInfo;
  ExistsInThisGroup: boolean;
}

@Component({
  selector: 'group-info-dialog',
  templateUrl: 'group-info-dialog.html',
})
export class GroupInfoDialogComponent {

  public OnViewUserInfo = new EventEmitter<UserInfo>();

  public OnJoinGroup = new EventEmitter<ConversationTemplate>();

  public OnViewAttachments = new EventEmitter<ConversationTemplate>();

  constructor(
    public dialogRef: MatDialogRef<ChatComponent>,
    public dialog: MatDialog ,
    @Inject(MAT_DIALOG_DATA) public data: GroupInfoData,
    public formatter: ConversationsFormatter,
    public conversationsService: ConversationsService,
    public ChangeNameDialog: MatDialog) { }

  public ViewUserInfo(user: UserInfo) {
    this.OnViewUserInfo.emit(user);
  }

  public IsJoined() {
    return this.data.ExistsInThisGroup;
  }

  public async ClearMessages() {
    await this.conversationsService.RemoveAllMessages(this.data.Conversation);
    this.dialogRef.close();
  }

  public RemoveGroup() {
    this.conversationsService.RemoveGroup(this.data.Conversation);
  }

  public JoinGroup() {
    this.OnJoinGroup.emit(this.data.Conversation);
  }

  public LeaveGroup() {
    this.conversationsService.Leave(this.data.Conversation);
    this.dialogRef.close();
  }

  public KickUser(user: UserInfo) {
    this.conversationsService.KickUser(user, this.data.Conversation);
  }

  public async BanUser(user: UserInfo) {
    await this.conversationsService.BanFromConversation(user, this.data.Conversation);
  }

  public async UnbanUser(user: UserInfo) {
    await this.conversationsService.UnbanFromConversation(user, this.data.Conversation);
  }

  public ViewAttachments() {
    const attachmentsDialogRef = this.dialog.open(ViewAttachmentsDialogComponent, {
      width: '450px',
      data: {
        conversation: this.data.Conversation
      }
    });
  }

  public IsCurrentUserCreatorOfConversation() {
    return this.data.user.id == this.data.Conversation.creator.id;
  }

  public async UpdateThumbnail(event: any) {
    await this.conversationsService.ChangeThumbnail(event.target.files[0], this.data.Conversation);
  }

  public ChangeName() {

    const groupInfoRef = this.ChangeNameDialog.open(ChangeNameDialogComponent, {
      width: '450px'
    });

    groupInfoRef.afterClosed().subscribe(
      async (name) => {
        if (name == null || name == '') {
          return;
        }

        await this.conversationsService.ChangeConversationName(name, this.data.Conversation);
      }
    )
  }

  public InviteUsers() {
    const dialogRef = this.dialog.open(FindUsersDialogComponent, {
      width: '350px',
      data: {
        conversationId: this.data.Conversation.conversationID
      }
    });

    dialogRef.beforeClosed().subscribe(users => {

      this.conversationsService.InviteUsersToGroup(users, this.data.Conversation);
    });
  }

}

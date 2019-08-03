import { AuthService } from "../Auth/AuthService";
import { ApiRequestsBuilder } from "../Requests/ApiRequestsBuilder";
import { UserInfo } from "../Data/UserInfo";
import { Injectable } from "@angular/core";
import { ConnectionManager, BanEvent } from "../Connections/ConnectionManager";

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  constructor(private requestsBuilder: ApiRequestsBuilder, private connectionManager: ConnectionManager, private auth: AuthService) { }

  //Fetches user info of specified user. If current user id specified,
  //updates user entry in AuthService.
  public async UpdateUserInfo(userId: string) : Promise<UserInfo> {
    let result = await this.requestsBuilder.GetUserById(userId);

    if (!result.isSuccessfull) {
      return null;
    }

    if (result.response.id == this.auth.User.id) {
      this.auth.User = result.response;
    }
    
    return result.response;
  }

  public async FindUsersByUsername(name: string): Promise<Array<UserInfo>>{
    let result = await this.requestsBuilder.FindUsersByUsername(name);

    if (!result.isSuccessfull) {
      return null;
    }

    if (result.response.usersFound == null) {
      return new Array<UserInfo>();

    } else {
      return [...result.response.usersFound];
    }
  }

  public async UpdateContacts() {
    let contacts = await this.requestsBuilder.GetContacts();

    if (!contacts.isSuccessfull) {
      return;
    }

    if (!contacts.response) {
      this.auth.Contacts = new Array<UserInfo>();
    } else {
      this.auth.Contacts = contacts.response;
    }
  }

  public HasContactWith(user: UserInfo) {
    return this.auth.Contacts.findIndex(x => x.id == user.id) != -1;
  }

  public async AddToContacts(user: UserInfo) {
    let result = await this.requestsBuilder.AddToContacts(user.id);

    if (!result.isSuccessfull) {
      return;
    }

    this.auth.Contacts.push(user);
  }

  public async RemoveFromContacts(user: UserInfo) {
    let result = await this.requestsBuilder.RemoveFromContacts(user.id);

    if (!result.isSuccessfull) {
      return;
    }

    let contactIndex = this.auth.Contacts.findIndex(x => x.id == user.id);

    if (contactIndex == -1) {
      return;
    }

    this.auth.Contacts.splice(contactIndex, 1);
  }

  public async BlockUser(user: UserInfo) {
    let result = await this.connectionManager.BlockUser(user.id, BanEvent.Banned);

    if (!result) {
      return;
    }

    user.isBlocked = true;
  }

  public async UnblockUser(user: UserInfo) {
    let result = await this.connectionManager.BlockUser(user.id, BanEvent.Unbanned);

    if (!result) {
      return;
    }

    user.isBlocked = false;
  }

  public async ChangeLastname(name: string) {
    let result = await this.requestsBuilder.ChangeCurrentUserLastName(name);

    if (!result.isSuccessfull) {
      return;
    }

    this.auth.User.lastName = name;
  }

  public async ChangeName(name: string) {
    let result = await this.requestsBuilder.ChangeCurrentUserName(name);

    if (!result.isSuccessfull) {
      return;
    }

    this.auth.User.name = name;
  }

  public async ChangeUsername(name: string) {
    let result = await this.requestsBuilder.ChangeUsername(name);

    if (!result.isSuccessfull) {
      return;
    }

    this.auth.User.userName = name;
  }

  public async UpdateProfilePicture(file: File, progressCallback: (value: number) => void) {
    let result = await this.requestsBuilder.UploadUserProfilePicture(file, progressCallback);

    if (!result.isSuccessfull) {
      return;
    }

    this.auth.User.imageUrl = result.response.thumbnailUrl;
    this.auth.User.fullImageUrl = result.response.fullImageUrl;
  };
}

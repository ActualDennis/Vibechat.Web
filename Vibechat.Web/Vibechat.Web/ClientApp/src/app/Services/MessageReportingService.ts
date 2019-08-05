import {SnackBarHelper} from "../Snackbar/SnackbarHelper";
import {Injectable} from "@angular/core";
import {SignalrConnection} from "../Connections/signalr-connection.service";

@Injectable({
  providedIn: 'root'
})
export class MessageReportingService {
  constructor(private snackbar: SnackBarHelper) {}

  public OnConnected(): void {
    this.snackbar.openSnackBar("Connected.", 1);
  }

  public OnDisconnected(): void {
    this.snackbar.openSnackBar(`Disconnected, retrying in ${SignalrConnection.DisconnectTimeout / 1000}s...`, SignalrConnection.DisconnectTimeout / 1000);
  }

  public TryingToReconnect(){
    this.snackbar.openSnackBar(`Failed to connect, retrying in ${SignalrConnection.DisconnectTimeout / 1000}s...`, SignalrConnection.DisconnectTimeout / 1000);
  }

  public OnConnecting() {
    this.snackbar.openSnackBar("Connecting...", 1.5);
  }

  public OnSendWhileDisconnected() {
    this.snackbar.openSnackBar("Couldn't perform actions while not connected...", 2.5);
  }

  public OnError(error: string): void {
    this.snackbar.openSnackBar(error, 2);
  }

  public OnWaitingForUserToComeOnline() {
    this.snackbar.openSnackBar("Waiting for user to come online...", 3);
  }

  public OnFailedToSubsribeToUserStatusChanges() {
    this.snackbar.openSnackBar("Last operation failed, please re-enter this chat.", 3);
  }

  public DisplayMessage(msg: string) {
    this.snackbar.openSnackBar(msg, 2);
  }
}

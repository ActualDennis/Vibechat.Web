import {Component} from '@angular/core';
import {FormControl, Validators} from '@angular/forms';
import {SnackBarHelper} from '../Snackbar/SnackbarHelper';
import {Router} from '@angular/router';
import {Api} from '../Services/Api/api.service';
import {AuthService} from "../Services/AuthService";
import * as firebase from "firebase/app";
import {MessageReportingService} from "../Services/MessageReportingService";

@Component({
  selector: 'login-view',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {

  constructor(private requestsBuilder: Api,
              private snackbar: SnackBarHelper,
              private router: Router, private auth: AuthService,
              private messages: MessageReportingService) {
    this.phoneNumber = new FormControl('',
      Validators.compose(
        [
          Validators.maxLength(AuthService.PHONENUMBER_LENGTH),
          Validators.minLength(AuthService.PHONENUMBER_LENGTH - 1)]
      ));
    this.smsCode = new FormControl('', Validators.maxLength(AuthService.SMSCODE_LENGTH));
    this.phoneNumber.valueChanges.subscribe(() => this.OnNumberChanged());
  }

  public phoneNumber: FormControl;
  public smsCode: FormControl;

  public canLogIn: boolean = true;

  public isCodeSent: boolean = false;

  private recaptchaVerifier: firebase.auth.RecaptchaVerifier;

  get Recaptcha(): firebase.auth.RecaptchaVerifier {

    if (!this.recaptchaVerifier) {
      this.recaptchaVerifier = new firebase.auth.RecaptchaVerifier('sign-in-button', { 'size': 'invisible' });
    }

    return this.recaptchaVerifier;
  }
  set Recaptcha(value: firebase.auth.RecaptchaVerifier) {
    this.recaptchaVerifier = value;
  }


  public async SignIn() {
    this.canLogIn = false;

    if (!await this.auth.SignIn(this.smsCode.value)) {
      this.messages.WrongSmsCode();
    }

    this.canLogIn = true;
  }

  public async SendCode() {

    this.canLogIn = false;

    let result = await this.auth.SendSmsCode(this.phoneNumber.value, this.Recaptcha);

    if (result) {
      this.isCodeSent = true;
    } else {
      this.messages.CouldntSendSms();
    }

    this.canLogIn = true;
  }

  public OnNumberChanged() {
    this.isCodeSent = false;
  }

  public GotoRegisterPage() : void {
    this.router.navigateByUrl('/register');
  }


}


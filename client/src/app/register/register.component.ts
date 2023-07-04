import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  @Output() cancelRegisterEvent = new EventEmitter();
  registerForm: FormGroup;
  maxDate: Date;
  validationErrors: string[] = [];

  constructor(private accountService: AccountService,
              private toastr: ToastrService,
              private fb: FormBuilder,
              private router: Router) {}
              
  ngOnInit(): void {
    this.initializeForm();
    this.maxDate = new Date();
    this.maxDate.setFullYear(this.maxDate.getFullYear() - 18);
  }

  initializeForm() {
    this.registerForm = this.fb.group({
      gender: ['male'],
      userName: [null, Validators.required],
      knownAs: [null, Validators.required],
      dateOfBirth: [null, Validators.required],
      city: [null, Validators.required],
      country: [null, Validators.required],
      password: [null, [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(8)
      ]],
      confirmPassword: [null, [
        Validators.required,
        this.matchValues('password')
      ]]
    });

    this.registerForm.get('password').valueChanges.subscribe(() => {
      this.registerForm.get('confirmPassword').updateValueAndValidity();
    });
  }

  matchValues(matchTo: string): ValidatorFn {
    return (control: AbstractControl) => {
      return control?.value === control?.parent?.get(matchTo)?.value ?
        null :
        {isMatching: true};
    };
  }

  register() {
   this.accountService.register(this.registerForm.value).subscribe(response => {
    this.router.navigateByUrl('/members');
   }, error => {
    this.validationErrors = error;
   });
  }

  cancel() {
    this.cancelRegisterEvent.emit(false);
  }
}

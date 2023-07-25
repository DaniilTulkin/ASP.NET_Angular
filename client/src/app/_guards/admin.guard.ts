import { Injectable } from '@angular/core';
import { CanActivate } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
  constructor(private accountService: AccountService,
              private toastrService: ToastrService) {}

  canActivate(): Observable<boolean> {
    return this.accountService.currentUser$.pipe(
      map(user => {
        if (user.roles.includes('Admin') || user.roles.includes("Moderator")) {
          return true;
        }
        this.toastrService.error('Ypu cannot enter this area');
        return false;
      })
    );
  }
  
}

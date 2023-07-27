import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, take } from 'rxjs';
import { Router } from '@angular/router';

import { environment } from 'src/environments/environment';
import { User } from '../_models/user';

@Injectable({
  providedIn: 'root'
})
export class PresenceService {
  hubUrl = environment.hubUrl;
  private hubConnection: HubConnection;
  private onlineUsersSource = new BehaviorSubject<string[]>([]);
  onlineUsers$ = this.onlineUsersSource.asObservable();

  constructor(private toastrSrvice: ToastrService,
              private router: Router) { }

  createHubConnection(user: User) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.hubUrl}presence`, {
        accessTokenFactory: () => user.token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .catch(error => console.log(error));

    this.hubConnection.on('UserIsOnline', userName => {
      this.onlineUsers$.pipe(take(1)).subscribe(userNames => {
        console.log(userNames);
        this.onlineUsersSource.next([...userNames, userName]);
      });
    });

    this.hubConnection.on('UserIsOffline', userName => {
      this.onlineUsers$.pipe(take(1)).subscribe(userNames => {
        console.log(userNames);
        this.onlineUsersSource.next([...userNames.filter(x => x !== userName)])
      });
    });

    this.hubConnection.on('GetOnlineUsers', (userNames: string[]) => {
      this.onlineUsersSource.next(userNames);
    });

    this.hubConnection.on('NewMessageReceived', ({userName, knownAs}) => {
      this.toastrSrvice.info(`${knownAs} has sent to you a new message`)
        .onTap
        .pipe(take(1))
        .subscribe(() => this.router.navigateByUrl(`/members/${userName}?tab=3`));
    });
  }

  stopHubConnection() {
    this.hubConnection
      .stop()
      .catch(error => console.log(error));
  }
}

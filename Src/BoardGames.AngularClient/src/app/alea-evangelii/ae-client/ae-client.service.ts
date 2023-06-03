import { Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr";
import { from as fromPromise } from 'rxjs';
import { AeHubConnection } from './ae-hub-connection';

@Injectable()
export class AeClientService {

  static readonly hubUrl = '/realtime/alea-evangelii'

  constructor() { }

  public startConnection( roomId: number | null = null ) {
    return fromPromise( this.startConnectionAsync( roomId ) )
  }

  private async startConnectionAsync( roomId: number | null ) {
    let url = roomId != null
      ? `${AeClientService.hubUrl}?room-id=${roomId}`
      : AeClientService.hubUrl

    let hubConnection = new signalR.HubConnectionBuilder()
      .withUrl( url )
      .build();

    await hubConnection
      .start()
      .then( () => console.log( 'Connection started' ) )
      .catch( err => console.log( 'Error while starting connection: ' + err ) )

    return new AeHubConnection( hubConnection )
  }

}

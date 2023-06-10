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
    let connection = new AeHubConnection( AeClientService.hubUrl, roomId )
    await connection.BeginConnection()
    return connection
  }

}

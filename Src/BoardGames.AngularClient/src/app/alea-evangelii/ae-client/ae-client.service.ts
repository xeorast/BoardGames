import { Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr";
import { from as fromPromise } from 'rxjs';
import { AeHubConnection } from './ae-hub-connection';
import { Player } from '../logic/types';

@Injectable()
export class AeClientService {

  static readonly hubUrl = '/realtime/alea-evangelii'

  constructor() { }

  public startConnection( roomId: number | null = null, preferredPlayer: Player | null = null ) {
    return fromPromise( this.startConnectionAsync( roomId, preferredPlayer ) )
  }

  private async startConnectionAsync( roomId: number | null, preferredPlayer: Player | null ) {
    let connection = new AeHubConnection( AeClientService.hubUrl, roomId, preferredPlayer )
    await connection.BeginConnection()
    return connection
  }

}

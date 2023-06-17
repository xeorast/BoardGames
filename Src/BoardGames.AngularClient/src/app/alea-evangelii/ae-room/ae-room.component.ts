import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, ParamMap, Router } from '@angular/router';
import { filter, withLatestFrom } from 'rxjs';
import { AeClientService } from '../ae-client/ae-client.service';
import { AeHubConnection } from '../ae-client/ae-hub-connection';
import { AeConnectionManagerService } from '../ae-connection-manager/ae-connection-manager.service';
import { Player } from '../logic/types';

@Component( {
  selector: 'app-ae-room',
  templateUrl: './ae-room.component.html',
  styleUrls: ['./ae-room.component.scss']
} )
export class AeRoomComponent implements OnInit, OnDestroy {

  public gameConnection?: AeHubConnection
  public get roomId() {
    return this.gameConnection?.roomId ?? null
  }
  public get game() {
    return this.gameConnection?.game ?? null
  }
  public preferredPlayer?: string
  public error?: string

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private gameClient: AeClientService,
    private gameConnMan: AeConnectionManagerService,
  ) { }

  ngOnInit(): void {
    this.route.url
      .pipe( withLatestFrom( this.route.paramMap, this.route.queryParamMap ) )
      .subscribe( ( [_url, paramMap, queryParamMap] ) => this.onUrlChange( paramMap, queryParamMap ) );

    this.router.events
      .pipe(
        filter( event => event instanceof NavigationEnd ),
        withLatestFrom( this.route.paramMap, this.route.queryParamMap )
      )
      .subscribe( ( [_event, paramMap, queryParamMap] ) => this.onUrlChange( paramMap, queryParamMap ) );
  }

  ngOnDestroy(): void {
    this.gameConnection?.disconnect()
  }

  onUrlChange( paramMap: ParamMap, queryParamMap: ParamMap ) {
    let id = paramMap.get( 'room-id' )
    let player = queryParamMap.get( 'player' ) as Player
    this.preferredPlayer = player
    this.connectTo( id, player )
  }

  connectTo( rommIdVal: string | null, player: Player | null ) {
    if ( this.gameConnMan.gameConnection && this.gameConnMan.gameConnection != this.gameConnection ) {
      this.gameConnection = this.gameConnMan.gameConnection
    }

    let roomId = rommIdVal == null ? null : Number( rommIdVal )
    if ( this.gameConnection && roomId == this.gameConnection.roomId
      && this.gameConnection.game && player == this.gameConnection.game.playingAs ) {
      return
    }

    this.gameClient.startConnection( roomId, player ).subscribe( con => {
      this.gameConnection?.disconnect()
      this.gameConnection = con

      this.gameConnMan.gameConnection = con
      con.joined.subscribe( joinRes => {
        this.router.navigate( ['/alea-evangelii', joinRes.roomId], { queryParams: { 'player': joinRes.joinedAs } } );
      } )
      con.disconnected.subscribe( reason => {
        this.error = reason
      } )
    } )
  }

}

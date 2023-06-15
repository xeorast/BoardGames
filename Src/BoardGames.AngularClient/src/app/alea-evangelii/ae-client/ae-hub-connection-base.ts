import * as signalR from "@microsoft/signalr";
import { from as fromPromise, Observable } from 'rxjs';
import { MoveResult, MoveSummary } from '../logic/game';
import { BoardEntry, Player, Position } from "../logic/types";

export abstract class AeHubConnectionBase {

    constructor(
        private hubUrl: string,
        private hubUrlRoomId: number | null,
        private hubUrlPreferPlayer: Player | null
    ) {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl( this.buildUrl() )
            .build();
    }
    private hubConnection: signalR.HubConnection
    private isCloseExpected = false

    protected abstract onJoinedAs( joinRes: JoinResult ): void
    protected abstract onMovePerformed( moveSummary: MoveSummary ): void
    protected abstract onDisconnect( disconnectReason: string ): void

    move( from: Position, to: Position ): Observable<MoveResult> {
        let promise = this.hubConnection.invoke( 'Move', from, to )
        return fromPromise( promise )
    }

    getBoardState(): Observable<BoardEntry[][]> {
        let promise = this.hubConnection.invoke( 'GetBoardState' )
        return fromPromise( promise )
    }

    disconnect() {
        this.isCloseExpected = true
        this.hubConnection.stop()
    }

    onClose( _error: Error | undefined ) {
        if ( this.isCloseExpected ) {
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl( this.buildUrl() )
            .withAutomaticReconnect()
            .build();

        this.BeginConnection()
            .then( () => console.log( 'Connection restarted' ) )
            .catch( err => console.log( 'Error while restarting connection: ' + err ) )
    }

    private _onJoinedAs( joinRes: JoinResult ) {
        this.hubUrlRoomId = joinRes.roomId
        this.hubUrlPreferPlayer = joinRes.joinedAs
        this.onJoinedAs( joinRes )
    }

    private _onDisconnect( reason: string ) {
        this.isCloseExpected = true
    }

    private buildUrl() {
        let url = this.hubUrl
        if ( this.hubUrlRoomId ) {
            url += `?room-id=${this.hubUrlRoomId}`
            if ( this.hubUrlPreferPlayer ) {
                url += `&prefer-player=${this.hubUrlPreferPlayer}`
            }
        }

        return url
    }

    public async BeginConnection() {
        await this.hubConnection
            .start()
            .then( () => console.log( 'Connection started' ) )
            .catch( err => console.log( 'Error while starting connection: ' + err ) )

        this.hubConnection.on( 'JoinedAs', jr => this._onJoinedAs( jr ) )
        this.hubConnection.on( 'MovePerformed', ms => this.onMovePerformed( ms ) )
        this.hubConnection.on( 'Disconnect', dr => this._onDisconnect( dr ) )
        this.hubConnection.on( 'Disconnect', dr => this.onDisconnect( dr ) )
        this.hubConnection.onclose( err => this.onClose( err ) )
    }

}

export interface JoinResult {
    joinedAs: Player
    nowPlaying: Player | null
    winner: Player | null
    roomId: number
    boardState: BoardEntry[][]
    lastMove: MoveSummary | null
}
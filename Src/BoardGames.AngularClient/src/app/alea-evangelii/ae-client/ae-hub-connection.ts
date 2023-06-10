import * as signalR from "@microsoft/signalr";
import { ReplaySubject, Subject } from "rxjs";
import { Board } from "../logic/board";
import { Game, MoveSummary } from "../logic/game";
import { AeHubConnectionBase, JoinResult } from "./ae-hub-connection-base";
import { Player } from "../logic/types";

export class AeHubConnection extends AeHubConnectionBase {

    constructor(
        hubUrl: string,
        hubUrlRoomId: number | null,
        hubUrlPreferPlayer: Player | null ) {
        super( hubUrl, hubUrlRoomId, hubUrlPreferPlayer )
    }

    private _game?: Game
    private _roomId?: number
    private _joinedSubject = new ReplaySubject<JoinResult>( 1 )
    private _movePerformedSubject = new Subject<MoveSummary>()
    private _disconnectedSubject = new Subject<string>()

    public get roomId() { return this._roomId }
    public get game() { return this._game }
    public get joined() { return this._joinedSubject.asObservable() }
    public get movePerformed() { return this._movePerformedSubject.asObservable() }
    public get disconnected() { return this._disconnectedSubject.asObservable() }
    public get gameEnded() { return this.game?.gameEnded }

    protected onJoinedAs( joinRes: JoinResult ): void {
        let board = new Board(
            joinRes.boardState,
            joinRes.boardState.length,
            joinRes.boardState[0].length )

        this._game = new Game(
            board,
            joinRes.joinedAs,
            joinRes.nowPlaying,
            joinRes.winner,
            ( from, to ) => this.move( from, to ) )

        this._roomId = joinRes.roomId
        this._joinedSubject.next( joinRes )
    }

    protected onMovePerformed( moveSummary: MoveSummary ): void {
        if ( !this._game ) {
            return
        }

        this._game.applyMove( moveSummary )
        this._movePerformedSubject.next( moveSummary )
    }

    protected onDisconnect( reason: string ): void {
        this._disconnectedSubject.next( reason )
    }

}
import { Game } from "../logic/game"

export class AeLastMoveArrowModel {
    constructor(
        private game: Game ) { }

    public get isDefined() {
        return this.game.lastMove != null
    }

    public get start() {
        return this.game.lastMove?.from
    }

    public get end() {
        return this.game.lastMove?.to
    }

    public get width() {
        if ( !( this.end && this.start ) ) {
            return 0
        }

        return this.end.column - this.start.column
    }

    public get height() {
        if ( !( this.end && this.start ) ) {
            return 0
        }

        return this.end.row - this.start.row
    }

    public get length() {
        return Math.sqrt( this.height * this.height + this.width * this.width )
    }

    public get angle() {
        return Math.atan2( this.height, this.width )
    }
}

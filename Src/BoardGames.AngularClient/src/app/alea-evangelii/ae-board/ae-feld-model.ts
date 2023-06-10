import { of } from "rxjs"
import { Board } from "../logic/board"
import { Game } from "../logic/game"
import { BoardEntry, Player, Position } from "../logic/types"

export class AeFieldModel {
    public row: number
    public column: number
    public isCorner: boolean

    constructor(
        private game: Game,
        pos: Position ) {
        this.row = pos.row
        this.column = pos.column
        this.isCorner = game.board.isForbidden( pos.row, pos.column )
    }

    public get piece() {
        return this.game.board.pieceAt( this.row, this.column )
    }

    public get canPieceBeMovedTo() {
        return this.game.canMoveTo( this.row, this.column )
    }

    public get canPieceBeMovedFrom() {
        return this.game.canMoveFrom( this.row, this.column )
    }

    public get isSelected() {
        return this.game.isSelected( this.row, this.column )
    }

    public moveTo() {
        this.game.moveTo( this.row, this.column )
    }

    public select() {
        this.game.selectPiece( this.row, this.column )
    }

    public static placeholderFields = AeFieldModel.generatePlaceholderFields()

    private static generatePlaceholderFields() {
        let state: BoardEntry[][] = Array.from( Array( 19 ), () => new Array( 19 ) )
        let board = new Board( state, 19, 19 )
        let game = new Game( board, null as any as Player, null, null, ( _1, _2 ) => of() )

        let fields: AeFieldModel[] = []
        for ( let row = 0; row < game.board.height; row++ ) {
            for ( let column = 0; column < game.board.width; column++ ) {
                fields.push( new AeFieldModel( game, { row, column } ) )
            }
        }
        return fields
    }

}

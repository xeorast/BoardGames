import { BoardEntry, Position } from "./types";

export class Board {
    constructor(
        public piecesMap: BoardEntry[][],
        public width: number,
        public height: number,
    ) { }

    pieceAt( row: number, column: number ) {
        return this.piecesMap[row][column]
    }

    private isCorner( row: number, column: number ) {
        return row == 0 && column == 0
            || row == 0 && column == this.width - 1
            || row == this.height - 1 && column == this.width - 1
            || row == this.height - 1 && column == 0;
    }

    isForbidden( row: number, column: number ) {
        return this.isCorner( row, column )
            || row == Math.floor( this.height / 2 ) && column == Math.floor( this.width / 2 )
    }

    getMovesPosibleFrom( fromRow: number, fromColumn: number ) {
        if ( !this.piecesMap ) {
            return []
        }

        let isCommander = this.pieceAt( fromRow, fromColumn ) == "commander"

        let moves: Position[] = []
        let row = fromRow + 1
        let column = fromColumn
        while ( row < this.piecesMap.length
            && this.piecesMap[row][column] == null
            && ( isCommander || !this.isForbidden( row, column ) ) ) {
            moves.push( { row, column } )
            row += 1
        }

        row = fromRow - 1
        while ( row >= 0
            && this.piecesMap[row][column] == null
            && ( isCommander || !this.isForbidden( row, column ) ) ) {
            moves.push( { row, column } )
            row -= 1
        }

        row = fromRow
        column = fromColumn + 1
        while ( column < this.piecesMap[row].length
            && this.piecesMap[row][column] == null
            && ( isCommander || !this.isForbidden( row, column ) ) ) {
            moves.push( { row, column } )
            column += 1
        }

        column = fromColumn - 1
        while ( column >= 0
            && this.piecesMap[row][column] == null
            && ( isCommander || !this.isForbidden( row, column ) ) ) {
            moves.push( { row, column } )
            column -= 1
        }
        return moves
    }

    move( from: Position, to: Position ) {
        let piece = this.piecesMap[from.row][from.column]
        this.piecesMap[from.row][from.column] = null
        this.piecesMap[to.row][to.column] = piece
    }

    capture( at: Position ) {
        this.piecesMap[at.row][at.column] = null
    }

}

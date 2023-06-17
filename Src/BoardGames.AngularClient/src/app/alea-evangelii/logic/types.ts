export type Player = "attacker" | "defender"

export type PieceType = "attacker" | "defender" | "commander"

export function OwnerOfPiece( piece: PieceType ): Player {
    if ( piece == 'attacker' ) {
        return 'attacker'
    }
    return 'defender'
}

export type BoardEntry = ( PieceType | null )

export interface Position {
    row: number,
    column: number
}

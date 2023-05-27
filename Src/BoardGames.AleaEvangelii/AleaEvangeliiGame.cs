﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BoardGames.AleaEvangelii;

public class AleaEvangeliiGame
{
	private readonly Board _board;

	public AleaEvangeliiGame()
	{
		_board = new();
	}

	public Player? Winner { get; private set; }
	public Player? NowPlaying { get; private set; } = Player.Attacker;

	private PieceType?[] GetColumn( int column )
	{
		var res = new PieceType?[_board.Heigth];
		for ( int row = 0; row < _board.Heigth; row++ )
		{
			res[row] = _board[new( row, column )];
		}
		return res;
	}

	private PieceType?[] GetRow( int row )
	{
		var res = new PieceType?[_board.Width];
		for ( int column = 0; column < _board.Width; column++ )
		{
			res[column] = _board[new( row, column )];
		}
		return res;
	}

	public bool CanMove( Position from, Position to )
	{
		var fieldState = _board[from];

		// empty field or wrong player or no move, disallow
		if ( fieldState is null || NowPlaying != fieldState.Value.Owner() || from == to )
		{
			return false;
		}

		// corners are oly for commander
		var piece = fieldState.Value;
		if ( piece is not PieceType.Commander && _board.IsCorner( to ) )
		{
			return false;
		}

		// same row, allow if nothing between
		if ( from.Row == to.Row )
		{
			var row = GetRow( from.Row ).AsSpan();
			var lowerFree = from.Column < to.Column ? from.Column + 1 : to.Column;
			var upperFree = from.Column > to.Column ? from.Column - 1 : to.Column;

			var freeRange = row[lowerFree..( upperFree + 1 )];
			return IsEmpty( freeRange );
		}

		// same column, allow if nothing between
		if ( from.Column == to.Column )
		{
			var column = GetColumn( from.Column ).AsSpan();
			var lowerFree = from.Row < to.Row ? from.Row + 1 : to.Row;
			var upperFree = from.Row > to.Row ? from.Row - 1 : to.Row;

			var freeRange = column[lowerFree..( upperFree + 1 )];
			return IsEmpty( freeRange );
		}

		// different row and column, disallow
		return false;
	}

	public MoveResult Move( Position from, Position to )
	{
		if ( !CanMove( from, to ) )
		{
			return MoveResult.Failed;
		}

		var piece = _board[from]!.Value;
		_board[from] = null;
		_board[to] = piece;

		var capturedPositions = _board.GetCapturedBy( to, piece );
		foreach ( var capturedPosition in capturedPositions )
		{
			_board[capturedPosition] = null;
		}

		if ( _board.DidCommanderEscape( to, piece ) )
		{
			Winner = Player.Defender;
			NowPlaying = null;
		}

		if ( _board.DidCaptureCommander( to, piece ) )
		{
			Winner = Player.Attacker;
			NowPlaying = null;
		}

		if ( NowPlaying is not null )
		{
			NowPlaying = NowPlaying.Value.Opposite();
		}

		GameEndData? endData = Winner is not null ? new( Winner.Value ) : null;

		return new MoveResult( new MoveSummary()
		{
			From = from,
			To = to,
			Captured = capturedPositions,
			GameEndData = endData,
		} );
	}

	private static bool IsEmpty( Span<PieceType?> range )
	{
		foreach ( var item in range )
		{
			if ( item is not null )
			{
				return false;
			}
		}

		return true;
	}

	public PieceType?[,] GetBoardState()
	{
		return _board.CloneFields();
	}

}

public class MoveSummary
{
	public required Position From { get; set; }
	public required Position To { get; set; }
	public required Position[] Captured { get; set; }
	public GameEndData? GameEndData { get; set; }
}

public record struct GameEndData( Player Winner );

public record struct MoveResult( MoveSummary? Summary )
{
	[MemberNotNullWhen( true, nameof( Summary ) )]
	public readonly bool Success => Summary is not null;

	[JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
	public MoveSummary? Summary { get; set; } = Summary;

	public static MoveResult Failed { get; } = new( null );
}

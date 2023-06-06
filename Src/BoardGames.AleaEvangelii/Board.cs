using System.Diagnostics.CodeAnalysis;

namespace BoardGames.AleaEvangelii;

public class Board
{
	public Board()
	{
		(_fields, Heigth, Width) = DefaultBoard.GetBoard();
	}
	public Board( PieceType?[,] state )
	{
		_fields = state;
		Heigth = state.GetLength( 0 );
		Width = state.GetLength( 1 );
	}

	private readonly PieceType?[,] _fields;
	public int Width { get; }
	public int Heigth { get; }
	public int BottomIndex => Heigth - 1;
	public int RightIndex => Width - 1;

	public PieceType? this[int row, int column]
	{
		get => _fields[row, column];
		set => _fields[row, column] = value;
	}
	public PieceType? this[Position pos]
	{
		get => _fields[pos.Row, pos.Column];
		set => _fields[pos.Row, pos.Column] = value;
	}

	public Position[] GetCapturedBy( Position targetPosition, PieceType movingPiece )
	{
		var pieceToCapture = movingPiece is PieceType.Attacker ? PieceType.Defender : PieceType.Attacker;

		var result = new List<Position>();

		// up
		Position oneUp = new( targetPosition.Row - 1, targetPosition.Column );
		Position twoUp = new( targetPosition.Row - 2, targetPosition.Column );
		if ( targetPosition.Row >= 2
			&& this[oneUp] == pieceToCapture
			&& ( movingPiece.Owner() == this[twoUp]?.Owner()
				|| IsCorner( twoUp ) ) )
		{
			result.Add( new( targetPosition.Row - 1, targetPosition.Column ) );
		}

		// down
		Position oneDown = new( targetPosition.Row + 1, targetPosition.Column );
		Position twoDown = new( targetPosition.Row + 2, targetPosition.Column );
		if ( targetPosition.Row <= BottomIndex - 2
			&& this[oneDown] == pieceToCapture
			&& ( movingPiece.Owner() == this[twoDown]?.Owner()
				|| IsCorner( twoDown ) ) )
		{
			result.Add( new( targetPosition.Row + 1, targetPosition.Column ) );
		}

		// right
		Position oneRight = new( targetPosition.Row, targetPosition.Column + 1 );
		Position twoRight = new( targetPosition.Row, targetPosition.Column + 2 );
		if ( targetPosition.Column <= RightIndex - 2
			&& this[oneRight] == pieceToCapture
			&& ( movingPiece.Owner() == this[twoRight]?.Owner()
				|| IsCorner( twoRight ) ) )
		{
			result.Add( new( targetPosition.Row, targetPosition.Column + 1 ) );
		}

		// left
		Position oneLeft = new( targetPosition.Row, targetPosition.Column - 1 );
		Position twoLeft = new( targetPosition.Row, targetPosition.Column - 2 );
		if ( targetPosition.Column >= 2
			&& this[oneLeft] == pieceToCapture
			&& ( movingPiece.Owner() == this[twoLeft]?.Owner()
				|| IsCorner( twoLeft ) ) )
		{
			result.Add( new( targetPosition.Row, targetPosition.Column - 1 ) );
		}

		return result.ToArray();
	}

	public bool DidCaptureCommander( Position targetPosition, PieceType movingPiece )
	{
		if ( movingPiece is not PieceType.Attacker )
		{
			return false;
		}

		if ( targetPosition.Row > 0 && this[targetPosition.Row - 1, targetPosition.Column] is PieceType.Commander )
		{
			return IsCommanderCaptured( new( targetPosition.Row - 1, targetPosition.Column ) );
		}

		if ( targetPosition.Row < BottomIndex && this[targetPosition.Row + 1, targetPosition.Column] is PieceType.Commander )
		{
			return IsCommanderCaptured( new( targetPosition.Row + 1, targetPosition.Column ) );
		}

		if ( targetPosition.Column > 0 && this[targetPosition.Row, targetPosition.Column - 1] is PieceType.Commander )
		{
			return IsCommanderCaptured( new( targetPosition.Row, targetPosition.Column - 1 ) );
		}

		if ( targetPosition.Column < RightIndex && this[targetPosition.Row, targetPosition.Column + 1] is PieceType.Commander )
		{
			return IsCommanderCaptured( new( targetPosition.Row, targetPosition.Column + 1 ) );
		}

		return false;
	}

	private bool IsCommanderCaptured( Position commanderPosition )
	{
		var left = commanderPosition.Column == 0
			|| this[commanderPosition.Row, commanderPosition.Column - 1] is PieceType.Attacker;

		var right = commanderPosition.Column == RightIndex
			|| this[commanderPosition.Row, commanderPosition.Column + 1] is PieceType.Attacker;

		var top = commanderPosition.Row == 0
			|| this[commanderPosition.Row - 1, commanderPosition.Column] is PieceType.Attacker;

		var bottom = commanderPosition.Row == BottomIndex
			|| this[commanderPosition.Row + 1, commanderPosition.Column] is PieceType.Attacker;

		return left && right && top && bottom;
	}

	public bool DidCommanderEscape( Position targetPosition, PieceType movingPiece )
	{
		return movingPiece is PieceType.Commander && IsCorner( targetPosition );
	}

	public bool IsCorner( Position position )
	{
		return position.Row == 0 && position.Column == 0
			|| position.Row == 0 && position.Column == RightIndex
			|| position.Row == BottomIndex && position.Column == RightIndex
			|| position.Row == BottomIndex && position.Column == 0;
	}

	public bool IsForbidden( Position position )
	{
		return IsCorner( position )
			|| position.Row == BottomIndex / 2 && position.Column == RightIndex / 2;
	}

	public PieceType?[,] CloneFields()
	{
		return (PieceType?[,])_fields.Clone();
	}

}

internal static class DefaultBoard
{
	static PieceType?[,]? fields;
	private static int rowsCount;
	private static int columnsCount;

	[MemberNotNull( nameof( fields ) )]
	private static void LoadBoard()
	{
		var dir = Path.GetDirectoryName( typeof( DefaultBoard ).Assembly.Location ) ?? "";
		var path = Path.Combine( dir, "AeInitialBoard.txt" );
		var rows = File.ReadAllLines( path );
		rowsCount = rows.Length;
		columnsCount = rows[0].Length;

		fields = new PieceType?[rowsCount, columnsCount];
		for ( int row = 0; row < rowsCount; row++ )
		{
			for ( int col = 0; col < columnsCount; col++ )
			{
				fields[row, col] = rows[row][col] switch
				{
					'A' => PieceType.Attacker,
					'D' => PieceType.Defender,
					'C' => PieceType.Commander,
					_ => null,
				};
			}
		}
	}

	public static (PieceType?[,] piece, int rowsCount, int columnsCount) GetBoard()
	{
		if ( fields is null )
		{
			LoadBoard();
		}

		var copy = (PieceType?[,])fields.Clone();
		return (copy, rowsCount, columnsCount);
	}

}

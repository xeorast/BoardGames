using BoardGames.AleaEvangelii;
using BoardGames.Server.Utils;
using Google.Cloud.Firestore;

namespace BoardGames.Server.Data.AleaEvangelii.Firestore;

[FirestoreData]
public record struct BoardStateRow( [property: FirestoreProperty] PieceType?[] RowValue );

[FirestoreData]
public record struct PositionFirestoreModel( [property: FirestoreProperty] int Row, [property: FirestoreProperty] int Column )
{
	public static explicit operator PositionFirestoreModel( Position position )
		=> new( position.Row, position.Column );

	public readonly Position ToPosition()
		=> new( Row, Column );
}

[FirestoreData]
public record struct GameEndDataFirestoreModel( [property: FirestoreProperty] Player Winner )
{
	public static explicit operator GameEndDataFirestoreModel( GameEndData data )
		=> new( data.Winner );

	public readonly GameEndData ToGameEndData()
		=> new( Winner );
}

[FirestoreData]
public class MoveSummaryFirestoreModel
{
	[FirestoreProperty]
	public required PositionFirestoreModel From { get; set; }

	[FirestoreProperty]
	public required PositionFirestoreModel To { get; set; }

	[FirestoreProperty]
	public required PositionFirestoreModel[] Captured { get; set; }

	[FirestoreProperty]
	public GameEndDataFirestoreModel? GameEndData { get; set; }

	public static MoveSummaryFirestoreModel CreateFrom( MoveSummary moveSummary )
	{
		return new()
		{
			Captured = moveSummary.Captured.Select( p => (PositionFirestoreModel)p ).ToArray(),
			From = (PositionFirestoreModel)moveSummary.From,
			To = (PositionFirestoreModel)moveSummary.To,
			GameEndData = (GameEndDataFirestoreModel?)moveSummary.GameEndData
		};
	}

	public MoveSummary ToSummary()
	{
		return new()
		{
			Captured = Captured.Select( p => p.ToPosition() ).ToArray(),
			From = From.ToPosition(),
			To = To.ToPosition(),
			GameEndData = GameEndData?.ToGameEndData()
		};
	}

}

[FirestoreData]
public class FirestoreAERoomModel
{
	public static HashSet<Player> PossiblePlayers { get; }
		= new() { Player.Attacker, Player.Defender };

	[FirestoreProperty]
	public Dictionary<string, Player> PresentPlayers { get; set; } = new();

	[FirestoreProperty]
	public Player? Winner { get; set; }

	[FirestoreProperty]
	public Player? NowPlaying { get; set; }

	[FirestoreProperty]
	public required BoardStateRow[] BoardState { get; set; }

	[FirestoreProperty]
	public required MoveSummaryFirestoreModel? LastMove { get; set; }

	[FirestoreProperty]
	public Dictionary<string, int> Captured { get; set; } = new();

	public AleaEvangeliiGame GetGame()
	{
		var stateArr = BoardState.Select( row => row.RowValue ).ToArray();
		Board board = new( stateArr.To2DArray() );

		var captured = Captured.ToDictionary( d => Enum.Parse<Player>( d.Key ), d => d.Value );
		return new( board, Winner, NowPlaying, LastMove?.ToSummary(), captured );
	}

	public static FirestoreAERoomModel Create()
		=> FromGame( new() );

	public static FirestoreAERoomModel FromGame( AleaEvangeliiGame game )
	{
		var stateArr = game.GetBoardState().ToArrayOfArrays();
		return new()
		{
			BoardState = stateArr.Select( row => new BoardStateRow( row ) ).ToArray(),
			NowPlaying = game.NowPlaying,
			Winner = game.Winner,
			LastMove = game.LastMove is null ? null : MoveSummaryFirestoreModel.CreateFrom( game.LastMove ),
			Captured = game.Captured.ToDictionary( d => d.Key.ToString(), d => d.Value ),
		};
	}

	public void PatchFromGame( AleaEvangeliiGame game )
	{
		var stateArr = game.GetBoardState().ToArrayOfArrays();
		BoardState = stateArr.Select( row => new BoardStateRow( row ) ).ToArray();
		NowPlaying = game.NowPlaying;
		Winner = game.Winner;
		LastMove = game.LastMove is null ? null : MoveSummaryFirestoreModel.CreateFrom( game.LastMove );
		Captured = game.Captured.ToDictionary( d => d.Key.ToString(), d => d.Value );
	}

}

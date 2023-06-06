using BoardGames.AleaEvangelii;
using BoardGames.Server.Utils;
using Google.Cloud.Firestore;

namespace BoardGames.Server.Data.AleaEvangelii.Firestore;

[FirestoreData]
public record struct BoardStateRow( [property: FirestoreProperty] PieceType?[] RowValue );

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

	public AleaEvangeliiGame GetGame()
	{
		var stateArr = BoardState.Select( row => row.RowValue ).ToArray();
		Board board = new( stateArr.To2DArray() );

		return new( board, Winner, NowPlaying );
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
			Winner = game.Winner
		};
	}

	public void PatchFromGame( AleaEvangeliiGame game )
	{
		var stateArr = game.GetBoardState().ToArrayOfArrays();
		BoardState = stateArr.Select( row => new BoardStateRow( row ) ).ToArray();
		NowPlaying = game.NowPlaying;
		Winner = game.Winner;
	}

}

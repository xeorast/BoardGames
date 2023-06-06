using BoardGames.AleaEvangelii;
using Google.Cloud.Firestore;

namespace BoardGames.Server.Data.AleaEvangelii.Firestore;

public interface IAsyncAERoomStorage
{
	Task<string> CreateRoom();
	Task<(AEJoinRoomResult result, Player player)> TryJoinRoom( string roomId, string playerId, Player? prefferedPlayer );
	Task LeaveRoom( string roomId, string playerId );
	Task<(bool success, AleaEvangeliiGame? game)> TryGetGame( string roomId );
	//// todo: dont allow saving the game returned in different transaction transaction
	//Task SaveGame( string roomId, AleaEvangeliiGame game );
	Task RemoveRoomIfEmpty( string roomId );

	Task<MoveResult?> PerformMove( string roomId, Player playingAs, Position from, Position to );
}

public class FirestoreAERoomStorage : IAsyncAERoomStorage
{
	private readonly FirestoreDb _firestoreDb;
	const string collectionPath = "game-states/alea-evangelii/rooms";

	public FirestoreAERoomStorage( FirestoreDb firestoreDb )
	{
		_firestoreDb = firestoreDb;
	}

	public async Task<string> CreateRoom()
	{
		var room = FirestoreAERoomModel.Create();
		bool created;
		string id;

		do
		{

			var ids = await GetExistingRoomIds();

			do
			{
				id = Random.Shared.Next( 1000, 10000 ).ToString();

			} while ( ids.Contains( id ) );

			DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( id );
			try
			{
				var res = await documentRef.CreateAsync( room );
				created = true;
			}
			catch ( Grpc.Core.RpcException e ) when ( e.StatusCode is Grpc.Core.StatusCode.AlreadyExists )
			{
				created = false;
			}
		}
		while ( !created );

		return id;
	}

	public async Task<(AEJoinRoomResult result, Player player)> TryJoinRoom( string roomId, string playerId, Player? prefferedPlayer )
	{
		DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );

		return await _firestoreDb.RunTransactionAsync( async transaction =>
		{
			DocumentSnapshot documentSnapshot = await transaction.GetSnapshotAsync( documentRef );

			if ( !documentSnapshot.Exists )
			{
				return (AEJoinRoomResult.RoomDoesNotExists, default);
			}

			var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();

			var possible = FirestoreAERoomModel.PossiblePlayers.Except( room.PresentPlayers.Values ).ToArray();
			if ( possible.Length == 0 )
			{
				return (AEJoinRoomResult.RoomIsFull, default);
			}

			var chosen = prefferedPlayer is not null && possible.Contains( prefferedPlayer.Value )
				? prefferedPlayer.Value
				: possible[Random.Shared.Next( 0, possible.Length )];

			room.PresentPlayers.Add( playerId, chosen );

			transaction.Set( documentRef, room, SetOptions.Overwrite );

			return (result: AEJoinRoomResult.Success, player: chosen);
		} );
	}

	public async Task LeaveRoom( string roomId, string playerId )
	{
		DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );

		await _firestoreDb.RunTransactionAsync( async transaction =>
		{
			DocumentSnapshot documentSnapshot = await transaction.GetSnapshotAsync( documentRef );

			if ( !documentSnapshot.Exists )
			{
				return;
			}

			var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();

			room.PresentPlayers.Remove( playerId );

			transaction.Set( documentRef, room, SetOptions.Overwrite );
		} );
	}

	public async Task<(bool success, AleaEvangeliiGame? game)> TryGetGame( string roomId )
	{
		DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );
		DocumentSnapshot documentSnapshot = await documentRef.GetSnapshotAsync();

		if ( !documentSnapshot.Exists )
		{
			return (false, null);
		}

		var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();
		return (true, room.GetGame());
	}

	//public async Task SaveGame( string roomId, AleaEvangeliiGame game )
	//{
	//	DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );

	//	await _firestoreDb.RunTransactionAsync( async transaction =>
	//	{
	//		DocumentSnapshot documentSnapshot = await transaction.GetSnapshotAsync( documentRef );
	//		var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();

	//		room.PatchFromGame( game );

	//		transaction.Set( documentRef, room, SetOptions.Overwrite );
	//	} );
	//}

	public async Task RemoveRoomIfEmpty( string roomId )
	{
		DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );

		await _firestoreDb.RunTransactionAsync( async transaction =>
		{
			DocumentSnapshot documentSnapshot = await transaction.GetSnapshotAsync( documentRef );

			if ( !documentSnapshot.Exists )
			{
				return;
			}

			var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();

			if ( room.PresentPlayers.Count == 0 && room.Winner != null )
			{
				transaction.Delete( documentRef );
			}
		} );
	}

	public async Task<MoveResult?> PerformMove( string roomId, Player playingAs, Position from, Position to )
	{
		DocumentReference documentRef = _firestoreDb.Collection( collectionPath ).Document( roomId );

		return await _firestoreDb.RunTransactionAsync<MoveResult?>( async transaction =>
		{
			DocumentSnapshot documentSnapshot = await transaction.GetSnapshotAsync( documentRef );
			if ( !documentSnapshot.Exists )
			{
				return null;
			}

			var room = documentSnapshot.ConvertTo<FirestoreAERoomModel>();
			var game = room.GetGame();

			if ( game.Winner is not null )
			{
				return MoveResult.Failed;
			}

			if ( game.NowPlaying != playingAs )
			{
				return MoveResult.Failed;
			}

			var moveResult = game.Move( from, to );

			room.PatchFromGame( game );

			if ( moveResult.Success )
			{
				transaction.Set( documentRef, room, SetOptions.Overwrite );
			}

			return moveResult;
		} );
	}

	private async Task<HashSet<string>> GetExistingRoomIds()
	{
		CollectionReference usersCollection = _firestoreDb.Collection( collectionPath );

		QuerySnapshot querySnapshot = await usersCollection.Select( FieldPath.DocumentId ).GetSnapshotAsync();

		return querySnapshot.Select( document => document.Id ).ToHashSet();
	}

}

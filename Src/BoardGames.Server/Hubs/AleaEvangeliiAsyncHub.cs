using BoardGames.AleaEvangelii;
using BoardGames.Server.Data.AleaEvangelii;
using BoardGames.Server.Data.AleaEvangelii.Firestore;
using BoardGames.Server.Utils;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace BoardGames.Server.Hubs;


public class AleaEvangeliiAsyncHub : Hub<IAleaEvangeliiClient>, IAleaEvangeliiServer
{
	public AleaEvangeliiAsyncHub( IAsyncAERoomStorage roomStorage )
	{
		_roomStorage = roomStorage;
	}

	private readonly IAsyncAERoomStorage _roomStorage;

	public bool SuccesfulyJoinedRoom { get => (bool)Context.Items["is-in-room"]!; set => Context.Items["is-in-room"] = value; }
	public string RoomId { get => (string)Context.Items["room-id"]!; set => Context.Items["room-id"] = value; }
	public Player PlayingAs { get => (Player)Context.Items["playing-as"]!; set => Context.Items["playing-as"] = value; }

	public override async Task OnConnectedAsync()
	{
		SuccesfulyJoinedRoom = false;

		var httpContext = Context.GetHttpContext();
		Debug.Assert( httpContext is not null );

		string? roomId = httpContext.Request.Query["room-id"];
		roomId ??= await _roomStorage.CreateRoom();

		Player playingAs;
		switch ( await _roomStorage.TryJoinRoom( roomId, Context.ConnectionId ) )
		{
			case (AEJoinRoomResult.RoomDoesNotExists, _ ):
				await Clients.Caller.Disconnect( "Room with specified id does not exist." );
				Context.Abort();
				return;

			case (AEJoinRoomResult.RoomIsFull, _ ):
				await Clients.Caller.Disconnect( "Room is full." );
				Context.Abort();
				return;

			case (AEJoinRoomResult.Success, var _playingAs ):
				playingAs = _playingAs;
				break;

			default:
				await Clients.Caller.Disconnect( "Unknown error." );
				Context.Abort();
				return;
		}

		if ( await _roomStorage.TryGetGame( roomId ) is not (success: true, game: AleaEvangeliiGame game ) )
		{
			await Clients.Caller.Disconnect( "Room with specified id does not exist." );
			Context.Abort();
			return;
		}

		SuccesfulyJoinedRoom = true;
		RoomId = roomId;
		PlayingAs = playingAs;

		var board = game.GetBoardState();

		await Groups.AddToGroupAsync( Context.ConnectionId, roomId.ToString() );

		AleaEvangeliiJoinResult joinResult = new(
			playingAs,
			roomId,
			game.NowPlaying,
			game.Winner,
			board.ToArrayOfArrays() );
		await Clients.Caller.JoinedAs( joinResult );
	}

	public override async Task OnDisconnectedAsync( Exception? exception )
	{
		if ( SuccesfulyJoinedRoom )
		{
			await _roomStorage.LeaveRoom( RoomId, Context.ConnectionId );

			await _roomStorage.RemoveRoomIfEmpty( RoomId );
		}

		await base.OnDisconnectedAsync( exception );
	}

	public async Task<MoveResult> Move( Position from, Position to )
	{
		//if ( await _roomStorage.TryGetGame( RoomId ) is not (success: true, game: AleaEvangeliiGame game ) )
		//{
		//	await Clients.Caller.Disconnect( "Room disappeared." );
		//	Context.Abort();
		//	return MoveResult.Failed;
		//}

		//if ( game.Winner is not null )
		//{
		//	return MoveResult.Failed;
		//}

		//if ( game.NowPlaying != PlayingAs )
		//{
		//	return MoveResult.Failed;
		//}

		//var moveResult = game.Move( from, to );

		//if ( moveResult.Success )
		//{
		//	await _roomStorage.SaveGame( RoomId, game );
		//	await Clients.OthersInGroup( RoomId.ToString() ).MovePerformed( moveResult.Summary );
		//}

		var moveResult = await _roomStorage.PerformMove( RoomId, PlayingAs, from, to );
		if ( moveResult is not MoveResult resultValue )
		{
			await Clients.Caller.Disconnect( "Room disappeared." );
			Context.Abort();
			return MoveResult.Failed;
		}

		if ( resultValue.Success )
		{
			await Clients.OthersInGroup( RoomId.ToString() ).MovePerformed( resultValue.Summary );
		}

		return resultValue;
	}

	public async Task<PieceType?[][]> GetBoardState()
	{
		if ( await _roomStorage.TryGetGame( RoomId ) is not (success: true, game: AleaEvangeliiGame game ) )
		{
			await Clients.Caller.Disconnect( "Room disappeared." );
			Context.Abort();
			return Array.Empty<PieceType?[]>();
		}

		var board = game.GetBoardState();
		return board.ToArrayOfArrays();
	}

}

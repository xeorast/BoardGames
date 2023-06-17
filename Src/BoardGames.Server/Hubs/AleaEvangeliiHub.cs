using BoardGames.AleaEvangelii;
using BoardGames.Server.Data.AleaEvangelii;
using BoardGames.Server.Utils;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace BoardGames.Server.Hubs;

public interface IAleaEvangeliiClient
{
	Task JoinedAs( AleaEvangeliiJoinResult join );
	Task MovePerformed( MoveSummary summary );
	Task Disconnect( string reason );
}

public record struct AleaEvangeliiJoinResult(
	Player JoinedAs,
	string RoomId,
	Player? NowPlaying,
	Player? Winner,
	PieceType?[][] BoardState,
	MoveSummary? LastMove,
	IReadOnlyDictionary<Player, int> Captured );

public interface IAleaEvangeliiServer
{
	Task<MoveResult> Move( Position from, Position to );
	Task<PieceType?[][]> GetBoardState();
}

public class AleaEvangeliiHub : Hub<IAleaEvangeliiClient>, IAleaEvangeliiServer
{
	public AleaEvangeliiHub( IAERoomStorage roomStorage )
	{
		_roomStorage = roomStorage;
	}

	private readonly IAERoomStorage _roomStorage;

	public bool SuccesfulyJoinedRoom { get => (bool)Context.Items["is-in-room"]!; set => Context.Items["is-in-room"] = value; }
	public string RoomId { get => (string)Context.Items["room-id"]!; set => Context.Items["room-id"] = value; }
	public Player PlayingAs { get => (Player)Context.Items["playing-as"]!; set => Context.Items["playing-as"] = value; }

	public override async Task OnConnectedAsync()
	{
		SuccesfulyJoinedRoom = false;

		var httpContext = Context.GetHttpContext();
		Debug.Assert( httpContext is not null );

		string? roomId = httpContext.Request.Query["room-id"];
		roomId ??= _roomStorage.CreateRoom();

		switch ( _roomStorage.TryJoinRoom( roomId, Context.ConnectionId, out var playingAs ) )
		{
			case AEJoinRoomResult.RoomDoesNotExists:
				await Clients.Caller.Disconnect( "Room with specified id does not exist." );
				Context.Abort();
				return;

			case AEJoinRoomResult.RoomIsFull:
				await Clients.Caller.Disconnect( "Room is full." );
				Context.Abort();
				return;

			default:
				break;
		}

		if ( !_roomStorage.TryGetGame( roomId, out var game ) )
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
			board.ToArrayOfArrays(),
			game.LastMove,
			game.Captured );
		await Clients.Caller.JoinedAs( joinResult );
	}

	public override async Task OnDisconnectedAsync( Exception? exception )
	{
		if ( SuccesfulyJoinedRoom )
		{
			_roomStorage.LeaveRoom( RoomId, Context.ConnectionId );

			if ( _roomStorage.TryGetGame( RoomId, out var game ) && game.Winner != null )
			{
				_roomStorage.RemoveRoomIfEmpty( RoomId );
			}
		}

		await base.OnDisconnectedAsync( exception );
	}

	public async Task<MoveResult> Move( Position from, Position to )
	{
		if ( !_roomStorage.TryGetGame( RoomId, out var game ) )
		{
			await Clients.Caller.Disconnect( "Room disappeared." );
			Context.Abort();
			return MoveResult.Failed;
		}

		if ( game.Winner is not null )
		{
			return MoveResult.Failed;
		}

		if ( game.NowPlaying != PlayingAs )
		{
			return MoveResult.Failed;
		}

		var moveResult = game.Move( from, to );
		if ( moveResult.Success )
		{
			await Clients.OthersInGroup( RoomId.ToString() ).MovePerformed( moveResult.Summary );
		}

		return moveResult;
	}

	public async Task<PieceType?[][]> GetBoardState()
	{
		await Task.CompletedTask;

		if ( !_roomStorage.TryGetGame( RoomId, out var game ) )
		{
			await Clients.Caller.Disconnect( "Room disappeared." );
			Context.Abort();
			return Array.Empty<PieceType?[]>();
		}

		var board = game.GetBoardState();
		return board.ToArrayOfArrays();
	}

}

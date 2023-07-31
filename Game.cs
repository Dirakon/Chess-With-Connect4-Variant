using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ChessWithConnect4;
using ChessWithConnect4.Multiplayer;
using Newtonsoft.Json;

public interface IPlayerGameState{}

public record NonPlayable : IPlayerGameState;

public record ChessPlayerChoosingPieceToMove : IPlayerGameState;

public record ChessPlayerTryingToMove(Position StartPosition, Move[] PossibleChessMoves): IPlayerGameState;

public record ChessPlayerAwaitingOpponentMove : IPlayerGameState;



public record CheckersPlayerChoosingColumnToDrop : IPlayerGameState;

public record CheckersPlayerAwaitingOpponentMove : IPlayerGameState;

public static class GameStateExtensions
{
	public static bool IsChessPlayer(this IPlayerGameState state)
	{
		return state is ChessPlayerTryingToMove or ChessPlayerChoosingPieceToMove or ChessPlayerAwaitingOpponentMove;
	}
}

public partial class Game : Control
{
	private IPlayerGameState _gameState = new NonPlayable();
	public static int MaxX = 7, MinX = 0, MaxY = 7, MinY = 0;
	private Dictionary<Position, TextureRect> _coordsToHighlighter;
	private Dictionary<Position, PieceSlot> _coordsToPieceSlot;
	private Dictionary<Position, ChessPieceType> _coordsToPiece;
	private HashSet<Position> CheckerPositions = new();
	[Export] public ChessboardColumn[] ChessboardColumns;
	public int TurnsLeft;

	public List<TextureRect> UsedHighlighters = new();
	[Export] public Color HighlighterPotentialMoveColor, HighlighterDefaultColor;

	public void ResetHighlighters()
	{
		foreach (var usedHighlighter in UsedHighlighters)
		{
			usedHighlighter.Modulate = HighlighterDefaultColor;
		}
		UsedHighlighters.Clear();
	}

	public void HighlightMoves(IEnumerable<Move> movesToHighlight)
	{
		foreach (var (position, _) in movesToHighlight)
		{
			var highlighter = _coordsToHighlighter[position];
			highlighter.Modulate = HighlighterPotentialMoveColor;
			UsedHighlighters.Add(highlighter);
		}
	}
	

	public void StartGame(Settings settings, bool isHost)
	{
		var jsonSettings = JsonConvert.SerializeObject(settings);
		GD.Print($"STARTING GAME WITH [host? {isHost}] [Settings: {jsonSettings}]");
		bool isChessPlayer = isHost? settings.HostPlaysChess : !settings.HostPlaysChess;
		_gameState = isChessPlayer ? new ChessPlayerChoosingPieceToMove() : new CheckersPlayerAwaitingOpponentMove();
		TurnsLeft = settings.MaxMoves;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_coordsToHighlighter = ChessboardColumns.SelectMany((column, x) =>
			column.OrderedHighlighters.Reverse().Select((button, y) => (coords: new Position(x, y), button))
		).ToDictionary(
			coordsAndButton => coordsAndButton.coords,
			coordsAndButton => coordsAndButton.button
		);
		
		_coordsToPieceSlot = ChessboardColumns.SelectMany((column, x) =>
			column.PieceSlots.Reverse().Select((pieceSlot, y) => (coords: new Position(x, y), pieceSlot))
		).ToDictionary(
			coordsAndPieceSlot => coordsAndPieceSlot.coords,
			coordsAndPieceSlot => coordsAndPieceSlot.pieceSlot
		);

		var coordsWithButton = ChessboardColumns.SelectMany((column, x) =>
			column.OrderedClickables.Reverse().Select((button, y) => (coords: new Position(x, y), button))
		).ToArray();

		foreach (var (position, button) in coordsWithButton) button.Pressed += () => OnButtonPressed(position);
		InitChessPieces();
	}

	public void InitChessPieces()
	{
		var chessPieces = new[]
		{
			(new Position(0,0), ChessPieceType.Rook),
			(new Position(1,0), ChessPieceType.Knight),
			(new Position(2,0), ChessPieceType.Bishop),
			(new Position(3,0), ChessPieceType.Queen),
			(new Position(4,0), ChessPieceType.King),
			(new Position(5,0), ChessPieceType.Bishop),
			(new Position(6,0), ChessPieceType.Knight),
			(new Position(7,0), ChessPieceType.Rook),
		}.Concat(
			Enumerable.Range(0, MaxX + 1). Select(x =>
				(new Position(x, 1), ChessPieceType.Pawn)
				)
			).ToArray();
		_coordsToPiece = new();
		foreach (var (position, chessPieceType) in chessPieces)
		{
			_coordsToPieceSlot[position].RenderChessPiece(chessPieceType);
			_coordsToPiece[position] = chessPieceType;
		}
	}

	public void HandleCheckerFall()
	{
		// TODO
	}

	public void HandlePotentialCheckerWin()
	{
		// TODO
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void ChessPlayerMoved(int fromX, int fromY, int toX, int toY)
	{
		switch (_gameState)
		{
			case CheckersPlayerChoosingColumnToDrop:
				GD.PushWarning($"Received unexpected chess player move: from ({fromX}, {fromY}) to ({toX}, {toY})");
				return;
			case ChessPlayerAwaitingOpponentMove:
				GD.PushWarning($"Somehow initiated a move while awaiting enemy move: from ({fromX}, {fromY}) to ({toX}, {toY})");
				return;
		}

		Position fromPosition = new(fromX, fromY);
		Position toPosition = new(toX, toY);
		
		var startPiece = _coordsToPiece[fromPosition];
		
		_coordsToPiece.Remove(fromPosition);
		_coordsToPieceSlot[fromPosition].RenderChessPiece(null);
			
		_coordsToPiece[toPosition] = startPiece;
		_coordsToPieceSlot[toPosition].RenderChessPiece(startPiece);

		HandleCheckerFall();
		HandlePotentialCheckerWin();

		_gameState = _gameState is CheckersPlayerAwaitingOpponentMove 
			? new CheckersPlayerChoosingColumnToDrop() : 
			new ChessPlayerAwaitingOpponentMove();
	}

	public void HandlePotentialChessWin()
	{
		// TODO
	}

	public void RenderTurnsLeft()
	{
		// TODO
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void CheckersPlayerDropped(int dropX)
	{
		switch (_gameState)
		{
			case NonPlayable:
				GD.PushWarning($"Trying to do something in non-playable state");
				return;
			case ChessPlayerTryingToMove:
			case ChessPlayerChoosingPieceToMove:
				GD.PushWarning($"Received unexpected chess player move: on {dropX}");
				return;
			case CheckersPlayerAwaitingOpponentMove:
				GD.PushWarning($"Somehow initiated a move while awaiting enemy move: on {dropX}");
				return;
		}
		
		// TODO

		HandleCheckerFall();
		HandlePotentialCheckerWin();
		TurnsLeft--;
		HandlePotentialChessWin();

		_gameState = _gameState is CheckersPlayerChoosingColumnToDrop 
			? new CheckersPlayerAwaitingOpponentMove() 
			: new ChessPlayerChoosingPieceToMove();
	}
	
	public void HandleChessMove(Position fromPosition, Position toPosition)
	{
		Rpc(MethodName.ChessPlayerMoved, fromPosition.X, fromPosition.Y, toPosition.X, toPosition.Y);
	}
	
	public Move[] CalculatePotentialMoves(Position position, ChessPieceType chessPieceType)
	{
		var theoreticalMoves = chessPieceType.GetMoves(position);
		return theoreticalMoves.Where(theoreticalMove => 
				theoreticalMove.PositionsRequiredToBeFree.All(positionRequiredToBeFree => 
					!_coordsToPiece.ContainsKey(positionRequiredToBeFree) 
					&& !CheckerPositions.Contains(positionRequiredToBeFree)
				)
			)
			.ToArray();
	}

	public void HandleChessPlayerClick(Position clickedPosition)
	{
		ResetHighlighters();
		{
			if (_gameState is ChessPlayerTryingToMove(var initiatorPosition, var potentialMoves))
			{
				Move? relevantMove = potentialMoves.FirstOrDefault(move => move.Position == clickedPosition, null);
				if (relevantMove is { } someMove)
				{
					HandleChessMove(initiatorPosition, someMove.Position);
					return;
				}

				_gameState = new ChessPlayerChoosingPieceToMove();
			}
		}

		ChessPieceType? chessPiece = _coordsToPiece.TryGetValue(clickedPosition, out var piece) ? piece : null;
		if (chessPiece is { } somePiece)
		{
			var potentialMoves = CalculatePotentialMoves(clickedPosition, somePiece);
			_gameState = new ChessPlayerTryingToMove(clickedPosition, potentialMoves);
			HighlightMoves(potentialMoves);
		}
		
	}
	public void HandleCheckerPlayerClick(Position clickedPosition)
	{
		Rpc(MethodName.CheckersPlayerDropped, clickedPosition.X);
	}

	public void OnButtonPressed(Position clickedPosition)
	{
		if (_gameState is NonPlayable or ChessPlayerAwaitingOpponentMove or CheckersPlayerAwaitingOpponentMove)
			return;
		if (_gameState.IsChessPlayer())
			HandleChessPlayerClick(clickedPosition);
		else
			HandleCheckerPlayerClick(clickedPosition);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

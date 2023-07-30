using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ChessWithConnect4;
using ChessWithConnect4.Multiplayer;
using Newtonsoft.Json;

public partial class Game : Control
{
	public static int MaxX = 7, MinX = 0, MaxY = 7, MinY = 7;
	private Dictionary<(int, int), TextureRect> _coordsToHighlighter;
	[Export] public ChessboardColumn[] ChessboardColumns;
	[Export] public ChessPiece[] ChessPieces;
	public bool IsChessPlayerTurn;
	public int TurnsLeft;
	public bool IsChessPlayer;
	

	public void StartGame(Settings settings, bool isHost)
	{
		GD.Print($"STARTING GAME WITH [host? {isHost}] [Settings: {JsonConvert.SerializeObject(settings)}]");
		IsChessPlayerTurn = isHost? settings.HostPlaysChess : !settings.HostPlaysChess;
		IsChessPlayerTurn = true;
		TurnsLeft = settings.MaxMoves;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_coordsToHighlighter = ChessboardColumns.SelectMany((column, y) =>
			column.OrderedHighlighters.Reverse().Select((button, x) => (coords: (x, y), button))
		).ToDictionary(
			coordsAndButton => coordsAndButton.coords,
			coordsAndButton => coordsAndButton.button
		);

		var coordsWithButton = ChessboardColumns.SelectMany((column, y) =>
			column.OrderedClickables.Reverse().Select((button, x) => (coords: (x, y), button))
		).ToArray();

		foreach (var ((x, y), button) in coordsWithButton) button.Pressed += () => OnButtonPressed(x, y);
		foreach (var piece in ChessPieces)
		{
			var (coords, button) = coordsWithButton.MinBy(coordsAndButton => 
				coordsAndButton.button.GlobalPosition.DistanceTo(piece.GlobalPosition));
			GD.Print(coords);
		}
	}

	public void OnButtonPressed(int x, int y)
	{
		GD.Print($"{x},{y}");
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

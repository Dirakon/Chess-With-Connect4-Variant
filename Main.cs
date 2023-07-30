using System.Collections.Generic;
using System.Linq;
using ChessWithConnect4.Multiplayer;
using Godot;
using Newtonsoft.Json;

namespace ChessWithConnect4;

public partial class Main : Control
{
    private Dictionary<(int, int), TextureRect> _coordsToHighlighter;
    [Export] public ChessboardColumn[] ChessboardColumns;
    [Export] public Control Game;
    [Export] public MatchmakingUi MatchmakingUi;

    public bool IsChessPlayerTurn;
    public int TurnsLeft;
    public bool IsChessPlayer;

    public void StartGame(Settings settings, bool isHost)
    {
        GD.Print($"STARTING GAME WITH [host? {isHost}] [Settings: {JsonConvert.SerializeObject(settings)}]");
        IsChessPlayerTurn = isHost? settings.HostPlaysChess : !settings.HostPlaysChess;
        IsChessPlayerTurn = true;
        TurnsLeft = settings.MaxMoves;
        MatchmakingUi.Visible = false;
        Game.Visible = true;
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
        );

        foreach (var ((x, y), button) in coordsWithButton) button.Pressed += () => OnButtonPressed(x, y);
    }

    public void OnButtonPressed(int x, int y)
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
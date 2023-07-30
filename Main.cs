using System.Collections.Generic;
using System.Linq;
using ChessWithConnect4.Multiplayer;
using Godot;
using Newtonsoft.Json;

namespace ChessWithConnect4;

public partial class Main : Control
{
    [Export] public Game Game;
    [Export] public MatchmakingUi MatchmakingUi;


    public void StartGame(Settings settings, bool isHost)
    {
        MatchmakingUi.Visible = false;
        Game.Visible = true;
        Game.StartGame(settings,isHost);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
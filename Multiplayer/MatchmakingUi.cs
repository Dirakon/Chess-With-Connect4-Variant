using System;
using Godot;
using Newtonsoft.Json;

namespace ChessWithConnect4.Multiplayer;

internal enum MatchmakingState
{
    SelectingMode,

    JoiningRoom,
    JoinedRoom,

    CreatingRoom,
    CreatedRoom,

    LeavingRoom
}

public partial class MatchmakingUi : Control
{
    private MatchmakingState _currentState = MatchmakingState.SelectingMode;
    [Export] public Control BackSegment;
    [Export] public CheckBox HostPlaysChess;

    [Export] public Control LogsSegment;
    [Export] public TextEdit LogsTextBox;
    [Export] public Main Main;
    [Export] public LineEdit MaxMovesTextBox;
    [Export] public Control ModeSelectorSegment;
    [Export] public MultiplayerClient MultiplayerClient;
    [Export] public LineEdit RoomCodeTextBox;
    [Export] public Control SettingsSegment;
    [Export] public Control StartSegment;
    [Export] public string WebsocketMatchmakingServerUrl;

    private void ResetUi()
    {
        var allUiSegments = new[] {LogsSegment, SettingsSegment, StartSegment, ModeSelectorSegment, BackSegment};
        foreach (var uiSegment in allUiSegments) uiSegment.Visible = false;
    }

    private void Render()
    {
        ResetUi();
        switch (_currentState)
        {
            case MatchmakingState.SelectingMode:
                ModeSelectorSegment.Visible = true;
                break;
            case MatchmakingState.JoinedRoom:
                BackSegment.Visible = true;
                break;
            case MatchmakingState.JoiningRoom:
            case MatchmakingState.CreatingRoom:
            case MatchmakingState.LeavingRoom:
                break;
            case MatchmakingState.CreatedRoom:
                SettingsSegment.Visible = true;
                StartSegment.Visible = true;
                BackSegment.Visible = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        LogsSegment.Visible = true;
    }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Render();
        MultiplayerClient.LobbyJoined += LobbyJoined;
        MultiplayerClient.LobbySealed += LobbySealed;
        MultiplayerClient.Connected += Connected;
        MultiplayerClient.Disconnected += Disconnected;

        Multiplayer.ConnectedToServer += MpServerConnected;
        Multiplayer.ConnectionFailed += MpServerDisconnect;
        Multiplayer.ServerDisconnected += MpServerDisconnect;
        Multiplayer.PeerConnected += MpPeerConnected;
        Multiplayer.PeerDisconnected += MpPeerDisconnected;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }


    public void OnCreateRoomButtonPressed()
    {
        MultiplayerClient.Start(WebsocketMatchmakingServerUrl);
        _currentState = MatchmakingState.CreatingRoom;
        Render();
        ClearLogs();
    }

    public void OnJoinRoomButtonPressed()
    {
        if (string.IsNullOrWhiteSpace(RoomCodeTextBox.Text))
        {
            AddLog("[ERROR] Room code not specified!");
            return;
        }

        MultiplayerClient.Start(WebsocketMatchmakingServerUrl, RoomCodeTextBox.Text);
        _currentState = MatchmakingState.JoiningRoom;
        Render();
        ClearLogs();
    }

    public void OnBackButtonPressed()
    {
        MultiplayerClient.Stop();

        _currentState = MatchmakingState.LeavingRoom;
        Render();
    }


    public void MpServerConnected()
    {
        AddLog($"[Multiplayer] Server connected (I am {MultiplayerClient.RtcMultiplayerPeer.GetUniqueId()})");
    }


    public void MpServerDisconnect()
    {
        AddLog($"[Multiplayer] Server disconnected (I am {MultiplayerClient.RtcMultiplayerPeer.GetUniqueId()})");
    }


    public void MpPeerConnected(long id)
    {
        AddLog($"[Multiplayer] Peer {id} connected");
    }


    public void MpPeerDisconnected(long id)
    {
        AddLog($"[Multiplayer] Peer {id} disconnected");
    }


    public void Connected(int id, bool _)
    {
        GD.Print($"Connected with id '{id}' in state {_currentState.ToString()}");
    }


    public void Disconnected()
    {
        switch (_currentState)
        {
            case MatchmakingState.JoiningRoom:
                AddLog(
                    $"[ERROR] Failed to join room... Reason is {MultiplayerClient.Code} - {MultiplayerClient.Reason}");
                break;
            case MatchmakingState.CreatingRoom:
                AddLog(
                    $"[ERROR] Failed to create room... Reason is {MultiplayerClient.Code} - {MultiplayerClient.Reason}");
                break;
            case MatchmakingState.JoinedRoom:
            case MatchmakingState.CreatedRoom:
            case MatchmakingState.LeavingRoom:
                ClearLogs();
                AddLog(
                    $"[INFO] Disconnected from the server... Reason is {MultiplayerClient.Code} - {MultiplayerClient.Reason}");
                break;
            case MatchmakingState.SelectingMode:
            default:
                AddLog(
                    $"[ERROR] Unexpectedly disconnected from server... Reason is {MultiplayerClient.Code} - {MultiplayerClient.Reason}");
                break;
        }

        _currentState = MatchmakingState.SelectingMode;
        Render();
    }


    public void LobbyJoined(string lobby)
    {
        switch (_currentState)
        {
            case MatchmakingState.JoiningRoom:
                AddLog($"[SUCCESS] Joined room {lobby}");
                _currentState = MatchmakingState.JoinedRoom;
                Render();
                break;
            case MatchmakingState.CreatingRoom:
                AddLog($"[SUCCESS] Created room! The code is '{lobby}'");
                _currentState = MatchmakingState.CreatedRoom;
                Render();
                break;
            case MatchmakingState.SelectingMode:
            case MatchmakingState.JoinedRoom:
            case MatchmakingState.CreatedRoom:
            case MatchmakingState.LeavingRoom:
            default:
                AddLog($"[ERROR] Unexpectedly connected to room with code {lobby} in state {_currentState.ToString()}");
                break;
        }
    }


    public void LobbySealed()
    {
        AddLog("[Signaling] Lobby has been sealed");
    }


    public void ClearLogs()
    {
        GD.Print("Logs cleared!");
        LogsTextBox.Text = "";
    }

    public void AddLog(string msg)
    {
        GD.Print(msg);
        LogsTextBox.Text += $"{msg}\n";
    }


    [Rpc(CallLocal = false)]
    public void OnHostStartedGame(string settingsAsJson)
    {
        var settings = JsonConvert.DeserializeObject<Settings>(settingsAsJson);
        Main.StartGame(settings, false);
    }

    public void OnStartButtonPressed()
    {
        var numberOfPeers = Multiplayer.GetPeers().Length;
        if (numberOfPeers != 1)
        {
            AddLog($"[ERROR] Invalid number of peers! Required 1, got {numberOfPeers}");
            return;
        }

        var settings = new Settings(
            int.Parse(string.IsNullOrWhiteSpace(MaxMovesTextBox.Text)
                ? MaxMovesTextBox.PlaceholderText
                : MaxMovesTextBox.Text),
            HostPlaysChess.ButtonPressed
        );
        Rpc(MethodName.OnHostStartedGame, JsonConvert.SerializeObject(settings));
        MultiplayerClient.SealLobby();
        Main.StartGame(settings, true);
    }
}
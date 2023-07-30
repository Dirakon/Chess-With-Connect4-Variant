using System;
using Godot;
using Godot.Collections;

namespace ChessWithConnect4.Multiplayer;

public enum Message
{
    Join,
    Id,
    PeerConnect,
    PeerDisconnect,
    Offer,
    Answer,
    Candidate,
    Seal
}

public partial class WebRtcClient : Node
{
    [Signal]
    public delegate void AnswerReceivedEventHandler(int id, string answer);

    [Signal]
    public delegate void CandidateReceivedEventHandler(int id, string mid, int index, string sdp);

    [Signal]
    public delegate void ConnectedEventHandler(int id, bool useMesh);

    [Signal]
    public delegate void DisconnectedEventHandler();

    [Signal]
    public delegate void LobbyJoinedEventHandler(string lobby);

    [Signal]
    public delegate void LobbySealedEventHandler();

    [Signal]
    public delegate void OfferReceivedEventHandler(int id, string offer);

    [Signal]
    public delegate void PeerConnectedEventHandler(int id);

    [Signal]
    public delegate void PeerDisconnectedEventHandler(int id);

    [Export] public bool Autojoin = true;
    [Export] public int Code = 1000;
    [Export] public string LobbyName = ""; // # Will create a new lobby if empty.
    [Export] public bool Mesh = true; // Will use the lobby host as relay otherwise.
    [Export] public string Reason = "Unknown";

    public WebSocketPeer Ws = new();


    public void ConnectToUrl(string url)
    {
        Close();
        Code = 1000;
        Reason = "Unknown";
        Ws.ConnectToUrl(url);
    }


    public void Close()
    {
        Ws.Close();
    }


    public bool ParseMsg()
    {
        var parsed = Json.ParseString(Ws.GetPacket().GetStringFromUtf8());
        if (parsed.Obj is not Dictionary msg || !msg.ContainsKey("type") ||
            !msg.ContainsKey("id") ||
            msg["data"].Obj is not string dataString)
            return false;
        var msgType = msg["type"].AsString();
        var msgId = msg["id"].AsString();

        if (!msgType.IsValidInt() || !msgId.IsValidInt())
            return false;

        var type = int.Parse(msgType);
        var srcId = int.Parse(msgId);

        switch ((Message) type)
        {
            case Message.Join:
                EmitSignal(SignalName.LobbyJoined, dataString);
                break;
            case Message.Id:
                EmitSignal(SignalName.Connected, srcId, dataString == "true");
                break;
            case Message.PeerConnect:
                EmitSignal(SignalName.PeerConnected, srcId);
                break;
            case Message.PeerDisconnect:
                EmitSignal(SignalName.PeerDisconnected, srcId);
                break;
            case Message.Offer:
                EmitSignal(SignalName.OfferReceived, srcId, dataString);
                break;
            case Message.Answer:
                EmitSignal(SignalName.AnswerReceived, srcId, dataString);
                break;
            case Message.Candidate:
                var candidate = dataString.Split("\n", false);
                if (candidate.Length != 3)
                    return false;
                if (!candidate[1].IsValidInt())
                    return false;
                EmitSignal(SignalName.CandidateReceived, srcId, candidate[0], int.Parse(candidate[1]), candidate[2]);
                break;
            case Message.Seal:
                EmitSignal(SignalName.LobbySealed);
                break;
            default:
                return false;
        }

        return true; // Parsed
    }

    public Error JoinLobby(string lobbyToJoin)
    {
        return SendMsg(Message.Join, Mesh ? 0 : 1, lobbyToJoin);
    }

    public Error SealLobby()
    {
        return SendMsg(Message.Seal, 0);
    }


    public Error SendCandidate(long id, string mid, long index, string sdp)
    {
        return SendMsg(Message.Candidate, id, $"\n{mid}\n{index}\n{sdp}");
    }


    public Error SendOffer(int id, string offer)
    {
        return SendMsg(Message.Offer, id, offer);
    }


    public Error SendAnswer(int id, string answer)
    {
        return SendMsg(Message.Answer, id, answer);
    }


    public Error SendMsg(Message type, long id, string data = "")
    {
        var myDictionary = new Dictionary();
        myDictionary["type"] = (int) type;
        myDictionary["id"] = id;
        myDictionary["data"] = data;
        return Ws.SendText(Json.Stringify(myDictionary));
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var oldState = Ws.GetReadyState();
        if (oldState == WebSocketPeer.State.Closed)
            return;
        Ws.Poll();

        var state = Ws.GetReadyState();
        if (state != oldState && state == WebSocketPeer.State.Open && Autojoin) JoinLobby(LobbyName);

        while (state == WebSocketPeer.State.Open && Ws.GetAvailablePacketCount() > 0)
            if (!ParseMsg())
                Console.WriteLine("Error parsing message from server.");
        if (state == WebSocketPeer.State.Closed)
        {
            Code = Ws.GetCloseCode();
            Reason = Ws.GetCloseReason();
            EmitSignal(SignalName.Disconnected);
        }
    }
}
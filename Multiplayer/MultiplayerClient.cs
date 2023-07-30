using System;
using Godot;
using Godot.Collections;
using GodotArray = Godot.Collections.Array;

public partial class MultiplayerClient : WebRtcClient
{
    private bool _isSealed;

    public WebRtcMultiplayerPeer RtcMultiplayerPeer = new();

    public MultiplayerClient()
    {
        Connected += _connected;
        Disconnected += _disconnected;

        OfferReceived += _offer_received;
        AnswerReceived += _answer_received;
        CandidateReceived += _candidate_received;

        LobbyJoined += _lobby_joined;
        LobbySealed += _lobby_sealed;
        PeerConnected += _peer_connected;
        PeerDisconnected += _peer_disconnected;
    }

    public void Start(string url, string lobby = "", bool mesh = true)
    {
        Stop();

        _isSealed = false;
        Mesh = mesh;
        LobbyName = lobby;
        ConnectToUrl(url);
    }


    public void Stop()
    {
        Multiplayer.MultiplayerPeer = null;
        RtcMultiplayerPeer.Close();
        Close();
    }


    public WebRtcPeerConnection _create_peer(int id)
    {
        var googleServerConfig = new Dictionary();
        googleServerConfig["urls"] = new GodotArray(new Variant[] {"stun:stun.l.google.com:19302"});

        var peerConfiguration = new Dictionary();
        peerConfiguration["iceServers"] = new GodotArray(new Variant[] {googleServerConfig});

        var peer = new WebRtcPeerConnection();
        peer.Initialize(peerConfiguration);
        peer.SessionDescriptionCreated += (type, data) => OfferCreated(type, data, id);
        peer.IceCandidateCreated += (mid, index, sdp) => NewIceCandidate(mid, index, sdp, id);

        RtcMultiplayerPeer.AddPeer(peer, id);
        if (id < RtcMultiplayerPeer.GetUniqueId()) // So lobby creator never creates offers.
            peer.CreateOffer();
        return peer;
    }


    public void NewIceCandidate(string midName, long indexName, string sdpName, int id)
    {
        SendCandidate(id, midName, indexName, sdpName);
    }


    public void OfferCreated(string type, string data, int id)
    {
        if (!RtcMultiplayerPeer.HasPeer(id))
            return;
        GD.Print($"created {type}");
        RtcMultiplayerPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().SetLocalDescription(type, data);
        if (type == "offer")
            SendOffer(id, data);
        else
            SendAnswer(id, data);
    }


    public void _connected(int id, bool useMesh)
    {
        GD.Print($"Connected {id}, mesh: {useMesh}");
        if (useMesh)
            RtcMultiplayerPeer.CreateMesh(id);
        else if (id == 1)
            RtcMultiplayerPeer.CreateServer();
        else
            RtcMultiplayerPeer.CreateClient(id);
        Multiplayer.MultiplayerPeer = RtcMultiplayerPeer;
    }


    public void _lobby_joined(string lobby)
    {
        LobbyName = lobby;
    }


    public void _lobby_sealed()
    {
        _isSealed = true;
    }


    public void _disconnected()
    {
        GD.Print($"Disconnected: {Code}: {Reason}");
        if (!_isSealed)
            Stop(); // Unexpected disconnect
    }


    public void _peer_connected(int id)
    {
        GD.Print("Peer connected ${id}");
        _create_peer(id);
    }


    public void _peer_disconnected(int id)
    {
        if (RtcMultiplayerPeer.HasPeer(id))
            RtcMultiplayerPeer.RemovePeer(id);
    }


    public void _offer_received(int id, string offer)
    {
        GD.Print($"Got offer: {id}");
        if (RtcMultiplayerPeer.HasPeer(id))
            RtcMultiplayerPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>()
                .SetRemoteDescription("offer", offer);
    }


    public void _answer_received(int id, string answer)
    {
        GD.Print($"Got answer: {id}");
        if (RtcMultiplayerPeer.HasPeer(id))
            RtcMultiplayerPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>()
                .SetRemoteDescription("answer", answer);
    }


    public void _candidate_received(int id, string mid, int index, string sdp)
    {
        if (RtcMultiplayerPeer.HasPeer(id))
            RtcMultiplayerPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().AddIceCandidate(mid, index, sdp);
    }
}
using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

//building on top of the Mirror NetworkManager class to add own custom room management
public class RoomNetworkManager : NetworkManager
{
    public const int maxPlayersPerRoom = 4;
    public const int minPlayersPerRoom = 1;

    //connnectionID is unique int per connection
    readonly Dictionary<string, Room> roomsByCode = new(); 
    readonly Dictionary<int, Room> roomOfConnection = new();
    int nextSlot;

    //called when server starts as well as when press host
    public override void OnStartServer()
    {
        base.OnStartServer();

        //register message handlers for custom messages
        NetworkServer.RegisterHandler<CreateRoomMessage>(OnCreateRoom);
        NetworkServer.RegisterHandler<JoinRoomMessage>(OnJoinRoom);
        NetworkServer.RegisterHandler<QuickJoinMessage>(OnQuickJoin);
        NetworkServer.RegisterHandler<ListRoomsMessage>(OnListRooms);
        NetworkServer.RegisterHandler<LeaveRoomMessage>((conn, msg) => RemoveMember(conn, notifyAndDestroy: true));
        NetworkServer.RegisterHandler<StartGameMessage>(OnStartGame);
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        roomsByCode.Clear();
        roomOfConnection.Clear();
        nextSlot = 0;
    }

    //when client disconnects from server remove them from room
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        RemoveMember(conn, notifyAndDestroy: false);
        base.OnServerDisconnect(conn);
    }

    private void OnCreateRoom(NetworkConnectionToClient conn, CreateRoomMessage message)
    {
        if(roomOfConnection.ContainsKey(conn.connectionId))
        {
            Debug.LogWarning("Client already in a room");
            return;
        }
        CreateAndJoin(conn, message.isPublic, message.playerName);
    }

    void CreateAndJoin(NetworkConnectionToClient conn, bool isPublic, string playerName)
    {
        
    }

    private void OnJoinRoom(NetworkConnectionToClient conn, JoinRoomMessage message)
    {
        throw new NotImplementedException();
    }
    private void OnQuickJoin(NetworkConnectionToClient conn, QuickJoinMessage message)
    {
        throw new NotImplementedException();
    }

    private void OnListRooms(NetworkConnectionToClient conn, ListRoomsMessage message)
    {
        throw new NotImplementedException();
    }
    private void RemoveMember(NetworkConnectionToClient conn, bool notifyAndDestroy)
    {
        throw new NotImplementedException();
    }
    private void OnStartGame(NetworkConnectionToClient conn, StartGameMessage message)
    {
        throw new NotImplementedException();
    }

}

class Room
{
    public string code;
    public string name;
    public bool isPublic;
    public bool started;
    public int slot;
    public Guid matchId = Guid.NewGuid();
    public List<NetworkConnectionToClient> members = new();
    public Dictionary<int, string> names = new(); //connectionID -> display name

    public NetworkConnectionToClient Host => members.Count > 0 ? members[0] : null; //Host is first member in list if they leave next player becomes host

}

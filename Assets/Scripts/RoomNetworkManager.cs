using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//building on top of the Mirror NetworkManager class to add own custom room management
public class RoomNetworkManager : NetworkManager
{
    public const int maxPlayersPerRoom = 4;
    public const int minPlayersToStart = 1;

    static readonly Color[] PlayerColors =
{
        new Color(0.30f, 0.65f, 1.00f), new Color(1.00f, 0.45f, 0.40f),
        new Color(0.45f, 0.90f, 0.50f), new Color(1.00f, 0.85f, 0.30f),
    };

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

        var room = new Room
        {
            code = NewCode(),
            name = playerName + "'s room",
            isPublic = isPublic,
            slot = nextSlot++ 
        };
        roomsByCode[room.code] = room;
        Debug.Log($"Room {room.code} created ({(isPublic ? "public" : "private")}) by {playerName}.");
        AddMember(room, conn, playerName);
    }

    void OnJoinRoom(NetworkConnectionToClient conn, JoinRoomMessage msg)
    {
        if (roomOfConnection.ContainsKey(conn.connectionId)) return;

        string code = (msg.code ?? "").Trim().ToUpper();
        if (!roomsByCode.TryGetValue(code, out var room)) return;
        if (room.started) return;
        if (room.members.Count >= maxPlayersPerRoom) return;

        AddMember(room, conn, msg.playerName);
    }

    void OnQuickJoin(NetworkConnectionToClient conn, QuickJoinMessage msg)
    {
        if (roomOfConnection.ContainsKey(conn.connectionId)) return;

        // Prefer the fullest joinable public room so rooms fill up instead of spreading thin.
        var room = roomsByCode.Values
            .Where(r => r.isPublic && !r.started && r.members.Count < maxPlayersPerRoom)
            .OrderByDescending(r => r.members.Count)
            .FirstOrDefault();

        if (room != null) AddMember(room, conn, msg.playerName);
        else CreateAndJoin(conn, true, msg.playerName);   // nothing open -> make one
    }

    void OnListRooms(NetworkConnectionToClient conn, ListRoomsMessage msg)
    {
        // Only PUBLIC, not-started, not-full rooms are ever revealed. Private rooms stay secret.
        var list = roomsByCode.Values
            .Where(r => r.isPublic && !r.started && r.members.Count < maxPlayersPerRoom)
            .Select(r => new RoomInfo { code = r.code, name = r.name, players = r.members.Count })
            .ToArray();

        conn.Send(new RoomListMessage { rooms = list });   // reply to just this one client
    }


    void OnStartGame(NetworkConnectionToClient conn, StartGameMessage msg)
    {
        if (!roomOfConnection.TryGetValue(conn.connectionId, out var room)) return;
        if (room.Host != conn) return;
        if (room.started) return;
        if (room.members.Count < minPlayersToStart) return; 

        room.started = true;
        Debug.Log($"Room {room.code} starting with {room.members.Count} players.");

        for (int i = 0; i < room.members.Count; i++)
        {
            var member = room.members[i];

            // Each room lives 100 units further along X than the previous one.
            Vector3 pos = new Vector3(room.slot * 100f + i * 2f, 1f, 0f);
            GameObject player = Instantiate(playerPrefab, pos, Quaternion.identity);

            // IMPORTANT: set data BEFORE AddPlayerForConnection. Spawn sends the object's
            // current SyncVar values to clients, so name/colour arrive together with the object.
            player.GetComponent<NetworkMatch>().matchId = room.matchId;  // which room can see it
            var sp = player.GetComponent<SimplePlayer>();
            if (sp != null)
            {
                sp.playerName = room.names[member.connectionId];
                sp.playerColor = PlayerColors[i % PlayerColors.Length];
            }

            NetworkServer.AddPlayerForConnection(member, player);
        }

        BroadcastState(room); 
    }


    void AddMember(Room room, NetworkConnectionToClient conn, string pname)
    {
        room.members.Add(conn);
        room.names[conn.connectionId] = pname;
        roomOfConnection[conn.connectionId] = room;
        Debug.Log($"{pname} joined room {room.code} ({room.members.Count}/{maxPlayersPerRoom}).");
        BroadcastState(room);
    }


    void RemoveMember(NetworkConnectionToClient conn, bool notifyAndDestroy)
    {
        if (!roomOfConnection.TryGetValue(conn.connectionId, out var room)) return;

        room.members.Remove(conn);
        room.names.Remove(conn.connectionId);
        roomOfConnection.Remove(conn.connectionId);

        if (notifyAndDestroy)
        {
            // Removes + destroys their player object on the server AND all clients.
            if (conn.identity != null)
                NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Destroy);

            // "code is empty" = "you're not in a room" -> client UI goes back to the main menu.
            conn.Send(new RoomStateMessage { code = "" });
        }

        if (room.members.Count == 0)
        {
            roomsByCode.Remove(room.code); // last one out deletes the room
        }
        else BroadcastState(room); // remaining players see the updated list / new host
    }


    void BroadcastState(Room room)
    {
        string[] names = room.members.Select(m => room.names[m.connectionId]).ToArray();

        foreach (var member in room.members)
        {
            member.Send(new RoomStateMessage
            {
                code = room.code,
                isPublic = room.isPublic,
                names = names,
                isHost = member == room.Host,
                started = room.started
            });
        }
    }

    string NewCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string code;
        do
        {
            var arr = new char[5];
            for (int i = 0; i < arr.Length; i++) arr[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            code = new string(arr);
        } while (roomsByCode.ContainsKey(code));   // re-roll on the (very unlikely) collision
        return code;
    }

    // Register message handlers here so nothing the server sends can arrive without a handler waiting for it
    public override void OnStartClient()
    {
        base.OnStartClient();
        RoomClient.RegisterHandlers();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        RoomClient.RaiseConnected();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        RoomClient.RaiseDisconnected();
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

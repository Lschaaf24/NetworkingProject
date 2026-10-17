using Mirror;

public struct RoomInfo
{
    public string code; 
    public string name;
    public int players;

}

//Client requests to server
public struct CreateRoomMessage : NetworkMessage { public bool isPublic; public string playerName; }
public struct JoinRoomMessage : NetworkMessage { public string code; public string playerName; }
public struct QuickJoinMessage : NetworkMessage { public string playerName; }
public struct ListRoomsMessage : NetworkMessage {}
public struct LeaveRoomMessage : NetworkMessage { }
public struct StartGameMessage : NetworkMessage { }


public struct RoomListMessage : NetworkMessage { public RoomInfo[] rooms; }
public struct RoomStateMessage : NetworkMessage
{
    public string code;
    public bool isPublic;
    public string[] names;
    public bool isHost;
    public bool started;
}

public struct RoomErrorMessage : NetworkMessage { public string reason; }

using Mirror;
using System;
using UnityEngine;

// Client handling of events for UI
public static class RoomClient 
{
    // Events the UI subscribes to
    public static event Action Connected;
    public static event Action Disconnected;
    public static event Action<RoomInfo[]> RoomListReceived;
    public static event Action<RoomStateMessage> RoomStateChanged;
    public static event Action<string> ErrorReceived;

    public static RoomStateMessage CurrentState { get; private set; }
    public static bool InRoom => !string.IsNullOrEmpty(CurrentState.code);


    // A handler is a function that calls when a message of that type arrives.
    public static void RegisterHandlers()
    {
        CurrentState = default;
        NetworkClient.ReplaceHandler<RoomListMessage>(m => RoomListReceived?.Invoke(m.rooms));
        NetworkClient.ReplaceHandler<RoomErrorMessage>(m => ErrorReceived?.Invoke(m.reason));
        NetworkClient.ReplaceHandler<RoomStateMessage>(m =>
        {
            CurrentState = m;                 
            RoomStateChanged?.Invoke(m);
        });
    }

    public static void RaiseConnected() => Connected?.Invoke();
    public static void RaiseDisconnected() { CurrentState = default; Disconnected?.Invoke(); }

    // NetworkClient.Send() sends to the server connected to
    public static void CreateRoom(bool isPublic, string name) =>
        NetworkClient.Send(new CreateRoomMessage { isPublic = isPublic, playerName = name });

    public static void JoinByCode(string code, string name) =>
        NetworkClient.Send(new JoinRoomMessage { code = code, playerName = name });

    public static void QuickJoin(string name) =>
        NetworkClient.Send(new QuickJoinMessage { playerName = name });

    public static void RequestList() => NetworkClient.Send(new ListRoomsMessage());
    public static void Leave() => NetworkClient.Send(new LeaveRoomMessage());
    public static void StartGame() => NetworkClient.Send(new StartGameMessage());
}

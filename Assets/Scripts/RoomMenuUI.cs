using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

public class RoomMenuUI : MonoBehaviour
{
    enum ScreenId { Connect, Main, Browser, JoinCode, Lobby, InGame }

    readonly Dictionary<ScreenId, GameObject> screens = new();
    ScreenId current;

    TMP_InputField nameInput, addressInput, codeInput;
    TextMeshProUGUI connectStatus, welcomeLabel, lobbyCode, lobbyVisibility, inGameLabel, toastLabel;
    RectTransform browserList, lobbyPlayers;
    Button startButton;
    GameObject toastRoot;
    float toastTimer, refreshTimer;

    string PlayerName => nameInput.text;

    // =================================================================================
    //  UNITY LIFECYCLE
    // =================================================================================
    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
    }

    // Subscribe to the events RoomClient raises. Always unsubscribe in OnDisable
    // (these are static events, so forgetting leaves dangling references).
    void OnEnable()
    {
        RoomClient.Connected += OnConnected;
        RoomClient.Disconnected += OnDisconnected;
        RoomClient.RoomListReceived += OnRoomList;
        RoomClient.RoomStateChanged += OnRoomState;
        RoomClient.ErrorReceived += OnError;
    }

    void OnDisable()
    {
        RoomClient.Connected -= OnConnected;
        RoomClient.Disconnected -= OnDisconnected;
        RoomClient.RoomListReceived -= OnRoomList;
        RoomClient.RoomStateChanged -= OnRoomState;
        RoomClient.ErrorReceived -= OnError;
    }

    void Start() => Show(NetworkClient.isConnected ? ScreenId.Main : ScreenId.Connect);

    void Update()
    {
        // Toast fade-out timer
        if (toastTimer > 0 && (toastTimer -= Time.deltaTime) <= 0) toastRoot.SetActive(false);

        // Public room list: poll the server every 3 s while the browser is open.
        // (Fine for small games. A bigger game would push updates instead.)
        if (current == ScreenId.Browser && (refreshTimer -= Time.deltaTime) <= 0)
        {
            refreshTimer = 3f;
            RoomClient.RequestList();
        }
    }

    // =================================================================================
    //  EVENTS FROM THE NETWORK  (the server's answers arrive here)
    // =================================================================================

    // Fired by RoomNetworkManager.OnClientConnect: we're connected to the server.
    void OnConnected()
    {
        connectStatus.text = "";
        Show(ScreenId.Main);
    }

    // Connection lost, server closed, or the address was wrong and it timed out.
    void OnDisconnected()
    {
        connectStatus.text = "Disconnected from server.";
        Show(ScreenId.Connect);
    }

    // The server tells us the state of the room we're in. THE central event of this UI.
    void OnRoomState(RoomStateMessage s)
    {
        if (string.IsNullOrEmpty(s.code))        // empty code = "you're not in a room"
        {
            Show(ScreenId.Main);
            return;
        }

        if (s.started)                           // host pressed Start
        {
            inGameLabel.text = $"Room {s.code}  -  {s.names.Length} player(s)";
            Show(ScreenId.InGame);
            return;
        }

        RefreshLobby(s);
        Show(ScreenId.Lobby);
    }

    void OnRoomList(RoomInfo[] rooms)
    {
        UIKit.Clear(browserList);
        if (rooms.Length == 0)
        {
            UIKit.Label(browserList, "No public rooms right now. Create one!", 20, TextAlignmentOptions.Center, 48);
            return;
        }

        foreach (var r in rooms)
        {
            var row = UIKit.Row(browserList, 56);
            var label = UIKit.Label(row, $"{r.name}     {r.players}/{RoomNetworkManager.maxPlayersPerRoom}", 22,
                TextAlignmentOptions.Left, 40);
            label.GetComponent<LayoutElement>().flexibleWidth = 1;   // label takes the leftover space

            string code = r.code;   // capture a copy for the lambda
            UIKit.MakeButton(row, "Join", UIKit.Accent, () => RoomClient.JoinByCode(code, PlayerName), 40, 90);
        }
    }

    void OnError(string reason) => Toast(reason);

    void RefreshLobby(RoomStateMessage s)
    {
        lobbyCode.text = s.code;
        lobbyVisibility.text = s.isPublic ? "Public room - anyone can find it" : "Private room - share the code with friends";

        UIKit.Clear(lobbyPlayers);
        for (int i = 0; i < RoomNetworkManager.maxPlayersPerRoom; i++)
        {
            bool filled = i < s.names.Length;
            var row = UIKit.Row(lobbyPlayers, 48);
            var l = UIKit.Label(row, filled ? s.names[i] + (i == 0 ? "   (host)" : "") : "Waiting for player...",
                22, TextAlignmentOptions.Left, 36);
            if (!filled) l.color = new Color(1, 1, 1, 0.4f);
        }

        startButton.gameObject.SetActive(s.isHost);   // only the host sees Start (server re-checks anyway)
    }

    // =================================================================================
    //  BUTTON ACTIONS
    // =================================================================================

    // HOST = start server and a local client in this same process.
    void OnHostClicked()
    {
        SaveName();
        connectStatus.text = "Starting host...";
        NetworkManager.singleton.StartHost();
    }

    // JOIN = start only a client and connect to the address typed in.
    // The result arrives later through OnConnected / OnDisconnected.
    void OnJoinServerClicked()
    {
        SaveName();
        connectStatus.text = "Connecting...";
        NetworkManager.singleton.networkAddress = addressInput.text.Trim();
        NetworkManager.singleton.StartClient();
    }

    void OnDisconnectClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected) NetworkManager.singleton.StopHost();
        else NetworkManager.singleton.StopClient();
        connectStatus.text = "";
        Show(ScreenId.Connect);
    }

    void OnCopyCode()
    {
        GUIUtility.systemCopyBuffer = lobbyCode.text;
        Toast("Code copied to clipboard");
    }

    void SaveName() => PlayerPrefs.SetString("playerName", PlayerName);

    // =================================================================================
    //  SCREEN SWITCHING
    // =================================================================================
    void Show(ScreenId id)
    {
        current = id;
        foreach (var kv in screens) kv.Value.SetActive(kv.Key == id);

        if (id == ScreenId.Main) welcomeLabel.text = $"Welcome, {PlayerName}";
        if (id == ScreenId.Browser) { refreshTimer = 3f; RoomClient.RequestList(); }
        if (id == ScreenId.JoinCode) codeInput.text = "";
    }

    void Toast(string msg)
    {
        toastLabel.text = msg;
        toastRoot.SetActive(true);
        toastTimer = 3f;
    }

    // =================================================================================
    //  BUILDING THE UI (layout only - nothing network related below this line)
    // =================================================================================
    void BuildUI()
    {
        var canvasGO = new GameObject("RoomMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGO.transform;

        // ---- Connect screen ----
        {
            var s = AddScreen(ScreenId.Connect, root, true);
            var card = UIKit.Card(s, 460);
            UIKit.Label(card, "ROOM DEMO", 40, TextAlignmentOptions.Center, 56);
            UIKit.Label(card, "Your name", 18, TextAlignmentOptions.Left, 24);
            nameInput = UIKit.Input(card, "Player name");
            nameInput.characterLimit = 16;
            nameInput.text = PlayerPrefs.GetString("playerName", "Player" + Random.Range(100, 999));
            UIKit.Label(card, "Server address (IP or hostname)", 18, TextAlignmentOptions.Left, 24);
            addressInput = UIKit.Input(card, "localhost");
            addressInput.text = "localhost";
            UIKit.MakeButton(card, "Join Server", UIKit.Accent, OnJoinServerClicked);
            UIKit.MakeButton(card, "Host Server + Play (this PC)", UIKit.Muted, OnHostClicked);
            connectStatus = UIKit.Label(card, "", 18, TextAlignmentOptions.Center, 28);
        }

        // ---- Main menu ----
        {
            var s = AddScreen(ScreenId.Main, root, true);
            var card = UIKit.Card(s, 460);
            UIKit.Label(card, "MAIN MENU", 36, TextAlignmentOptions.Center, 52);
            welcomeLabel = UIKit.Label(card, "", 20, TextAlignmentOptions.Center, 28);
            UIKit.MakeButton(card, "Quick Join (public)", UIKit.Accent, () => RoomClient.QuickJoin(PlayerName));
            UIKit.MakeButton(card, "Browse Public Rooms", UIKit.Muted, () => Show(ScreenId.Browser));
            UIKit.MakeButton(card, "Join With Code", UIKit.Muted, () => Show(ScreenId.JoinCode));
            UIKit.MakeButton(card, "Create Public Room", UIKit.Muted, () => RoomClient.CreateRoom(true, PlayerName));
            UIKit.MakeButton(card, "Create Private Room", UIKit.Muted, () => RoomClient.CreateRoom(false, PlayerName));
            UIKit.MakeButton(card, "Disconnect", UIKit.Danger, OnDisconnectClicked);
        }

        // ---- Public room browser ----
        {
            var s = AddScreen(ScreenId.Browser, root, true);
            var card = UIKit.Card(s, 560);
            UIKit.Label(card, "PUBLIC ROOMS", 36, TextAlignmentOptions.Center, 52);
            browserList = UIKit.ScrollList(card, 300);
            UIKit.MakeButton(card, "Refresh", UIKit.Muted, RoomClient.RequestList);
            UIKit.MakeButton(card, "Back", UIKit.Danger, () => Show(ScreenId.Main));
        }

        // ---- Join with code ----
        {
            var s = AddScreen(ScreenId.JoinCode, root, true);
            var card = UIKit.Card(s, 460);
            UIKit.Label(card, "JOIN WITH CODE", 36, TextAlignmentOptions.Center, 52);
            codeInput = UIKit.Input(card, "Enter 5-letter code");
            codeInput.characterLimit = 5;
            codeInput.onValidateInput = (text, index, ch) => char.ToUpper(ch);   // force capitals as you type
            UIKit.MakeButton(card, "Join", UIKit.Accent, () => RoomClient.JoinByCode(codeInput.text, PlayerName));
            UIKit.MakeButton(card, "Back", UIKit.Danger, () => Show(ScreenId.Main));
        }

        // ---- Room lobby ----
        {
            var s = AddScreen(ScreenId.Lobby, root, true);
            var card = UIKit.Card(s, 520);
            UIKit.Label(card, "ROOM CODE", 20, TextAlignmentOptions.Center, 26);
            lobbyCode = UIKit.Label(card, "-----", 64, TextAlignmentOptions.Center, 80);
            lobbyCode.characterSpacing = 12;
            lobbyVisibility = UIKit.Label(card, "", 18, TextAlignmentOptions.Center, 26);
            UIKit.MakeButton(card, "Copy Code", UIKit.Muted, OnCopyCode, 40);
            lobbyPlayers = UIKit.VList(card);
            startButton = UIKit.MakeButton(card, "START GAME", UIKit.Accent, RoomClient.StartGame);
            UIKit.MakeButton(card, "Leave Room", UIKit.Danger, RoomClient.Leave);
        }

        // ---- In-game overlay (small, top-left, no background so the game stays visible) ----
        {
            var s = AddScreen(ScreenId.InGame, root, false);
            var card = UIKit.Card(s, 300, new Vector2(0, 1), new Vector2(16, -16));
            inGameLabel = UIKit.Label(card, "", 20, TextAlignmentOptions.Left, 28);
            UIKit.MakeButton(card, "Leave Room", UIKit.Danger, RoomClient.Leave, 40);
        }

        BuildToast(root);
    }

    RectTransform AddScreen(ScreenId id, Transform parent, bool background)
    {
        var rt = UIKit.MakeScreen(parent, id.ToString(), background);
        screens[id] = rt.gameObject;
        return rt;
    }

    void BuildToast(Transform canvas)
    {
        toastRoot = new GameObject("Toast", typeof(RectTransform), typeof(Image));
        toastRoot.transform.SetParent(canvas, false);
        var rt = (RectTransform)toastRoot.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 40);
        rt.sizeDelta = new Vector2(520, 52);
        var img = toastRoot.GetComponent<Image>();
        img.color = UIKit.Danger; img.raycastTarget = false;

        toastLabel = UIKit.Label(rt, "", 20, TextAlignmentOptions.Center, 52);
        UIKit.Fill(toastLabel.rectTransform);
        toastRoot.SetActive(false);
    }

    // uGUI buttons only work if the scene has an EventSystem. Create one if missing.
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;   // (Unity 2021.3.18+/2022+)
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}

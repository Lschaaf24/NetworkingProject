using Mirror;
using TMPro;
using UnityEngine;

public class RoomMenuController : MonoBehaviour
{
    [Header("Screens (one root GameObject per screen)")]
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject browserPanel;
    [SerializeField] private GameObject joinCodePanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject inGamePanel;

    [Header("Connect screen")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_Text connectStatus;

    [Header("Main menu")]
    [SerializeField] private TMP_Text welcomeLabel;

    [Header("Public room browser")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject emptyListLabel;
    [SerializeField] private RoomRowUI roomRowPrefab;

    [Header("Join with code")]
    [SerializeField] private TMP_InputField codeInput;

    [Header("Lobby")]
    [SerializeField] private TMP_Text lobbyCodeLabel;
    [SerializeField] private TMP_Text lobbyVisibilityLabel;
    [SerializeField] private TMP_Text[] playerSlots;         
    [SerializeField] private GameObject startButton;        

    [Header("In game")]
    [SerializeField] private TMP_Text inGameLabel;


    enum ScreenId { Connect, Main, Browser, JoinCode, Lobby, InGame }
    ScreenId current;
    float refreshTimer;


    string PlayerName => nameInput.text;


    void Awake()
    {
        ValidateReferences();

        nameInput.characterLimit = 16;
        nameInput.text = PlayerPrefs.GetString("playerName", "Player" + Random.Range(100, 999));
        if (string.IsNullOrWhiteSpace(addressInput.text)) addressInput.text = "localhost";

        codeInput.characterLimit = 5;
        codeInput.onValidateInput = (text, index, ch) => char.ToUpper(ch);   // force capitals while typing

    }

    void OnEnable()
    {
        RoomClient.Connected += OnConnected;
        RoomClient.Disconnected += OnDisconnected;
        RoomClient.RoomListReceived += OnRoomList;
        RoomClient.RoomStateChanged += OnRoomState;
    }

    void OnDisable()
    {
        RoomClient.Connected -= OnConnected;
        RoomClient.Disconnected -= OnDisconnected;
        RoomClient.RoomListReceived -= OnRoomList;
        RoomClient.RoomStateChanged -= OnRoomState;
    }

    void Start() => Show(NetworkClient.isConnected ? ScreenId.Main : ScreenId.Connect);

    void Update()
    {

        // Poll the public room list every 3 seconds while the browser is open.
        if (current == ScreenId.Browser && (refreshTimer -= Time.deltaTime) <= 0)
        {
            refreshTimer = 3f;
            RoomClient.RequestList();
        }
    }

    public void OnHostClicked()
    {
        SaveName();
        connectStatus.text = "Starting host...";
        NetworkManager.singleton.StartHost();
    }

    public void OnJoinServerClicked()
    {
        SaveName();
        connectStatus.text = "Connecting...";
        NetworkManager.singleton.networkAddress = addressInput.text.Trim();
        NetworkManager.singleton.StartClient();
    }

    // Main menu
    public void OnQuickJoinClicked() => RoomClient.QuickJoin(PlayerName);
    public void OnCreatePublicClicked() => RoomClient.CreateRoom(true, PlayerName);
    public void OnCreatePrivateClicked() => RoomClient.CreateRoom(false, PlayerName);
    public void OnOpenBrowserClicked() => Show(ScreenId.Browser);
    public void OnOpenJoinCodeClicked() => Show(ScreenId.JoinCode);

    public void OnDisconnectClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected) NetworkManager.singleton.StopHost();
        else NetworkManager.singleton.StopClient();
        connectStatus.text = "";
        Show(ScreenId.Connect);
    }

    //Browser
    public void OnBackToMainClicked() => Show(ScreenId.Main);
    public void OnRefreshClicked() => RoomClient.RequestList();
    public void OnJoinCodeConfirmClicked() => RoomClient.JoinByCode(codeInput.text, PlayerName);

    //Lobby
    public void OnStartGameClicked() => RoomClient.StartGame();
    public void OnLeaveRoomClicked() => RoomClient.Leave();

    public void OnCopyCodeClicked()
    {
        GUIUtility.systemCopyBuffer = lobbyCodeLabel.text;
    }


    void OnConnected()
    {
        connectStatus.text = "";
        Show(ScreenId.Main);
    }

    void OnDisconnected()
    {
        connectStatus.text = "Disconnected from server.";
        Show(ScreenId.Connect);
    }

    void OnRoomState(RoomStateMessage s)
    {
        if (string.IsNullOrEmpty(s.code)) { Show(ScreenId.Main); return; }

        if (s.started)
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
        for (int i = roomListContent.childCount - 1; i >= 0; i--)
            Destroy(roomListContent.GetChild(i).gameObject);

        if (emptyListLabel != null) emptyListLabel.SetActive(rooms.Length == 0);

        foreach (var info in rooms)
        {
            RoomRowUI row = Instantiate(roomRowPrefab, roomListContent);
            row.Setup(info, code => RoomClient.JoinByCode(code, PlayerName));
        }
    }

    void RefreshLobby(RoomStateMessage s)
    {
        lobbyCodeLabel.text = s.code;
        lobbyVisibilityLabel.text = s.isPublic ? "Public room - anyone can find it"
                                               : "Private room - share the code with friends";

        for (int i = 0; i < playerSlots.Length; i++)
        {
            bool filled = i < s.names.Length;
            playerSlots[i].text = filled ? s.names[i] + (i == 0 ? "   (host)" : "") : "Waiting for player...";
            playerSlots[i].alpha = filled ? 1f : 0.4f;
        }

        startButton.SetActive(s.isHost);
    }

    void Show(ScreenId id)
    {
        current = id;
        connectPanel.SetActive(id == ScreenId.Connect);
        mainPanel.SetActive(id == ScreenId.Main);
        browserPanel.SetActive(id == ScreenId.Browser);
        joinCodePanel.SetActive(id == ScreenId.JoinCode);
        lobbyPanel.SetActive(id == ScreenId.Lobby);
        inGamePanel.SetActive(id == ScreenId.InGame);

        if (id == ScreenId.Main) welcomeLabel.text = $"Welcome, {PlayerName}";
        if (id == ScreenId.Browser) { refreshTimer = 3f; RoomClient.RequestList(); }
        if (id == ScreenId.JoinCode) codeInput.text = "";
    }


    void SaveName() => PlayerPrefs.SetString("playerName", PlayerName);

    void ValidateReferences()
    {
        var required = new (string name, UnityEngine.Object obj)[]
        {
            ("connectPanel", connectPanel), ("mainPanel", mainPanel), ("browserPanel", browserPanel),
            ("joinCodePanel", joinCodePanel), ("lobbyPanel", lobbyPanel), ("inGamePanel", inGamePanel),
            ("nameInput", nameInput), ("addressInput", addressInput), ("connectStatus", connectStatus),
            ("welcomeLabel", welcomeLabel), ("roomListContent", roomListContent), ("roomRowPrefab", roomRowPrefab),
            ("codeInput", codeInput), ("lobbyCodeLabel", lobbyCodeLabel), ("lobbyVisibilityLabel", lobbyVisibilityLabel),
            ("startButton", startButton), ("inGameLabel", inGameLabel),
        };
        foreach (var (n, o) in required)
            if (o == null) Debug.LogError($"[RoomMenuController] '{n}' is not assigned in the Inspector.", this);

        if (playerSlots == null || playerSlots.Length == 0)
            Debug.LogError("[RoomMenuController] 'playerSlots' needs 4 text objects assigned.", this);
    }
}

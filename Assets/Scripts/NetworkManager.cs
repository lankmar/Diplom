using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;


public class NetworkManager : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private Text connectionText;
    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private Camera sceneCamera;
    [SerializeField]
    private GameObject[] playerModel;
    [SerializeField]
    private GameObject serverWindow;
    [SerializeField]
    private GameObject messageWindow;
    [SerializeField]
    private GameObject sightImage;
    [SerializeField]
    private InputField username;
    [SerializeField]
    private InputField roomName;
    [SerializeField]
    private InputField roomList;
    [SerializeField]
    private InputField messagesLog;
    [SerializeField]
    public BackView backImage;

    private GameObject player;
    private Queue<string> messages;
    private const int messageCount = 10;
    private string nickNamePrefKey = "PlayerName";

    public Dictionary<string, int> players;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        messages = new Queue<string>(messageCount);
        if (PlayerPrefs.HasKey(nickNamePrefKey))
        {
            username.text = PlayerPrefs.GetString(nickNamePrefKey);
        }
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        connectionText.text = "Connecting to lobby...";


        players = new Dictionary<string, int>();

        resultPanel.active = false;
        timer = gameTime;
        timerText.gameObject.active = false;

        exit.onClick.AddListener(ExitApp);
    }

    /// <summary>
    /// Called on the client when you have successfully connected to a master server.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// Called on the client when the connection was lost or you disconnected from the server.
    /// </summary>
    /// <param name="cause">DisconnectCause data associated with this disconnect.</param>
    public override void OnDisconnected(DisconnectCause cause)
    {
        connectionText.text = cause.ToString();
    }

    /// <summary>
    /// Callback function on joined lobby.
    /// </summary>
    public override void OnJoinedLobby()
    {
        serverWindow.SetActive(true);
        connectionText.text = "";
    }

    /// <summary>
    /// Callback function on reveived room list update.
    /// </summary>
    /// <param name="rooms">List of RoomInfo.</param>
    public override void OnRoomListUpdate(List<RoomInfo> rooms)
    {
        roomList.text = "";
        foreach (RoomInfo room in rooms)
        {
            roomList.text += room.Name + "\n";
        }
    }

    /// <summary>
    /// The button click callback function for join room.
    /// </summary>
    public void JoinRoom()
    {
        serverWindow.SetActive(false);
        connectionText.text = "Joining room...";
        PhotonNetwork.LocalPlayer.NickName = username.text;
        PlayerPrefs.SetString(nickNamePrefKey, username.text);
        RoomOptions roomOptions = new RoomOptions()
        {
            IsVisible = true,
            MaxPlayers = 8
        };
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinOrCreateRoom(roomName.text, roomOptions, TypedLobby.Default);
            backImage.Actived(false);
        }
        else
        {
            connectionText.text = "PhotonNetwork connection is not ready, try restart it.";
        }
    }

    /// <summary>
    /// Callback function on joined room.
    /// </summary>
    public override void OnJoinedRoom()
    {
        connectionText.text = "";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Respawn(0.0f);
    }

    /// <summary>
    /// Start spawn or respawn a player.
    /// </summary>
    /// <param name="spawnTime">Time waited before spawn a player.</param>
    void Respawn(float spawnTime)
    {
        sightImage.SetActive(false);
        sceneCamera.enabled = true;
        StartCoroutine(RespawnCoroutine(spawnTime));
    }

    /// <summary>
    /// The coroutine function to spawn player.
    /// </summary>
    /// <param name="spawnTime">Time waited before spawn a player.</param>
    IEnumerator RespawnCoroutine(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        messageWindow.SetActive(true);
        sightImage.SetActive(true);
        int playerIndex = UnityEngine.Random.Range(0, playerModel.Length);
        int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        player = PhotonNetwork.Instantiate(playerModel[playerIndex].name, spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation, 0);
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.RespawnEvent += Respawn;
        playerHealth.AddMessageEvent += AddMessage;
        sceneCamera.enabled = false;
        if (spawnTime == 0)
        {
            AddMessage("Player " + PhotonNetwork.LocalPlayer.NickName + " Joined Game.");
        }
        else
        {
            AddMessage("Player " + PhotonNetwork.LocalPlayer.NickName + " Respawned.");
        }
    }

    /// <summary>
    /// Add message to message panel.
    /// </summary>
    /// <param name="message">The message that we want to add.</param>
    void AddMessage(string message)
    {
        photonView.RPC("AddMessage_RPC", RpcTarget.All, message);
    }

    /// <summary>
    /// RPC function to call add message for each client.
    /// </summary>
    /// <param name="message">The message that we want to add.</param>
    [PunRPC]
    void AddMessage_RPC(string message)
    {
        messages.Enqueue(message);
        if (messages.Count > messageCount)
        {
            messages.Dequeue();
        }
        messagesLog.text = "";
        foreach (string m in messages)
        {
            messagesLog.text += m + "\n";
        }
    }

    /// <summary>
    /// Callback function when other player disconnected.
    /// </summary>
    public override void OnPlayerLeftRoom(Player other)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AddMessage("Player " + other.NickName + " Left Game.");
        }
    }


    [SerializeField]
    private Text winnerText;
    [SerializeField]
    private Text secondPlayerText;
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private Button exit;

    [SerializeField]
    private Text timerText;

    [SerializeField] public int maxPLayer = 2;
    [SerializeField] private float gameTime = 40;

    public bool isGameStart = false;
    public bool isGameFinish = false;
    private float timer;

    void Update()
    {
        if (isGameFinish)
        {
            return;
        }
        if (isGameStart)
        {
            timerText.gameObject.active = true;
            timer = timer - Time.deltaTime;
            var ts = TimeSpan.FromSeconds(timer);
            timerText.text = $"{ts.Minutes} : {ts.Seconds}";
            if (timer <= 0)
            {
                isGameStart = false;
                GameFinish();
            }
        }
    }

    public void StartGame()
    {
        var playerList = PhotonNetwork.PlayerList;
        foreach (var item in playerList)
        {
            if (players.ContainsKey(item.NickName))
                players[item.NickName] = 0;
            else
                players.Add(item.NickName, 0);
        }
        if (isGameStart) return;

        timer = gameTime;
        isGameStart = true;
    }

    public void GameFinish()
    {
        timerText.gameObject.active = false;
        resultPanel.active = true;
        isGameStart = false;
        isGameFinish = true;
        var sortedDict = from entry in players orderby entry.Value ascending select entry;
        Debug.Log("GameFinish");
        winnerText.text = ($"{sortedDict.ElementAt(1).Key} - kill {sortedDict.ElementAt(1).Value} ");
        secondPlayerText.text = ($"{sortedDict.ElementAt(0).Key} - kill {sortedDict.ElementAt(0).Value} ");
        Time.timeScale = 0;
    }

    public void AddFrag(string playerName)
    {
        Debug.Log($"AddFrag {playerName}");
        players[playerName] += 1;
    }

    private void ExitApp()
    {
        OnLeftRoom();
        Application.Quit();
    }
    public override void OnLeftRoom()
    {
        
    }
}

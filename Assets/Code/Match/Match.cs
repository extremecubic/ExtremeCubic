using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum GameMode
{
	KingOfTheHill,
	TurfWar,
}

public class Match : Photon.MonoBehaviour
{
	public static Match instance       { get; private set; }
	public bool matchStarted           { get; private set; }
	public CameraController gameCamera { get; private set; }

	[Header("GAME REFERENCES")]
	[SerializeField] Level _level; public Level level { get { return _level; } }
	
	ScoreUI        _scoreUI;
	StartCounterUI _counterUI;
	WinnerUI       _winnerUI;
	MessagePromt   _msgPromt;

	MusicManager _musicManager;

	public GameMode currentGameModeType { get; private set; }
	IGameMode _currentGameMode;
	
	void Awake()
	{
		instance = this;

		gameCamera = FindObjectOfType<CameraController>();
	}

	void Start()
	{
		InGameUI UI = InGameUI.instance;
		_scoreUI    = UI.scoreUI;
		_counterUI  = UI.startCounterUI;
		_winnerUI   = UI.winnerUI;
		_msgPromt   = UI.msgPromt;

		_musicManager = MusicManager.instance;

		if (Constants.onlineGame)		
			SetupMatchOnline();
					
		if (!Constants.onlineGame)
			SetupMatchLocal();
	}

	void OnDestroy()
	{
		instance = null;	
	}

	void SetupMatchOnline()
	{		
		int numPlayer = PhotonNetwork.room.PlayerCount;

		currentGameModeType = (GameMode)PhotonNetwork.player.CustomProperties[Constants.MATCH_GAME_MODE];

		if (currentGameModeType == GameMode.KingOfTheHill)
			_currentGameMode = GetComponent<GameModeLastMan>();
		else if (currentGameModeType == GameMode.TurfWar)
			_currentGameMode = GetComponent<GameModeTurfWar>();

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		_scoreUI.Setup(numPlayer);

		// tell all clients to start the game
		// this is sent via server and the RPC wont be sent 
		// untill all players are loaded in to the scene
		if (PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkStartGame", PhotonTargets.AllViaServer, PhotonNetwork.time);

#if DEBUG_TOOLS
		new GameObject("Photon Debug", typeof(PhotonLagSimulationGui));
#endif
	}

	void SetupMatchLocal()
	{		
		int numPlayer = 4;

		// only have one gamemode for now
		_currentGameMode = GetComponent<GameModeLastMan>();

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		_scoreUI.Setup(numPlayer);

		_level.StartGameLocal();

		_counterUI.StartCount(0, 3, () => matchStarted = true);
	}

	// will be called on all clients when all players are loaded
	// in to the scene
	[PunRPC]
	void NetworkStartGame(double delta)
	{
		// init master class that have last say in all collisions(will only be called on the server)
		FindObjectOfType<CollisionTracker>().ManualStart();

		// create level (player creation is here for now aswell)
		_level.StartGameOnline();

		// start countdown
		_counterUI.StartCount(delta, 3, () => { matchStarted = true; _currentGameMode.OnRoundStart(); });
	}

	// called from character when it is created
	[PunRPC]
	public void RegisterPlayer(int id, string nickName, string viewName)
	{
		// register a player by id for scorekepping and ui
		_currentGameMode.OnPlayerRegistred(id);
		_scoreUI.RegisterPlayer(id, nickName, viewName);
	}

	// called from character(only on server in online play) 
	public void OnPlayerDie(int playerId)
	{
		_currentGameMode.OnPlayerDie(playerId);
	}
	
	// called from gamemode (called on all clients)
	// just used for setting UI and setting feedback
	public void OnRoundOver(int winnerId, int score)
	{
		_musicManager.StopSharedPowerUpLoop(0.5f);
		_scoreUI.UpdateScore(winnerId, score);
	}
	
	// only call from master client
	// this coRoutine will then send rpc to tell 
	// all clients to start counter for next round
	public void SetCoundownToRoundRestart(float delay)
	{				
		// do small delay before we reset to new round
		Timing.RunCoroutine(_resetDelay(delay));		
	}

	// callback from character when someone disconnects, called locally on all clients
	public void OnPlayerLeft(int id)
	{
		if (PhotonNetwork.playerList.Length == 1)
		{
			ShowLastPlayerMessage();
			return;
		}

		_currentGameMode.OnPlayerLeft(id);
		_scoreUI.DisableUIOfDisconnectedPlayer(id);
	}

	// called from gamemode to all clients
	[PunRPC]
	public void NetworkMatchOver(int id)
	{
		_winnerUI.ShowWinner(_scoreUI.GetUserNameFromID(id));
	}
	
	// called from the coroutine that handle delay before next round should start
	[PunRPC]
	void NetworkStartNewRound(double delta)
	{		
		matchStarted = false;

		_currentGameMode.OnRoundRestarted();

		// reset level(character resapwn is here aswell for now)
		_level.ResetRound();

		// restart start timer
		_counterUI.StartCount(delta, 3, () => { matchStarted = true; _currentGameMode.OnRoundStart(); });
	}	

	IEnumerator<float> _resetDelay(float delay)
	{
		float timer = 0;
		while (timer < delay)
		{
			timer += Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}

		if (Constants.onlineGame)
		    photonView.RPC("NetworkStartNewRound", PhotonTargets.All, PhotonNetwork.time);

		if (!Constants.onlineGame)
			NetworkStartNewRound(0);
	}

	void ShowLastPlayerMessage()
	{
		matchStarted = false;
		_msgPromt.SetAndShow("All Players have left the room!\n Returning to menu!", () =>
		{
			PhotonHelpers.ClearPlayerProperties(PhotonNetwork.player);
			PhotonNetwork.LeaveRoom();
			MainMenuSystem.reclaimPlayerUI = false;
			MainMenuSystem.startPage = Constants.SCREEN_START;
			UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
		});

	}
}

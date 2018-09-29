using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

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

	IGameMode _currentGameMode;
	
	void Awake()
	{
		instance = this;

		// only have one gamemode for now
		_currentGameMode = GetComponent<GameModeLastMan>();
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

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		_scoreUI.Setup(numPlayer);

		if (PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkStartGame", PhotonTargets.AllViaServer, PhotonNetwork.time);

#if DEBUG_TOOLS
		new GameObject("Photon Debug", typeof(PhotonLagSimulationGui));
#endif
	}

	void SetupMatchLocal()
	{		
		int numPlayer = 4;

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		_scoreUI.Setup(numPlayer);

		_level.StartGameLocal();

		_counterUI.StartCount(0, 3, () => matchStarted = true);
	}

	// called from character(only on server in online play) 
	public void OnPlayerDie(int playerId)
	{
		_currentGameMode.OnPlayerDie(playerId);
	}
	
	// called from gamemode (called on all clients)
	public void OnRoundOver(int winnerId, int score)
	{
		_musicManager.StopSharedPowerUpLoop(0.5f);
		_scoreUI.UpdateScore(winnerId, score);
	}
	
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

	[PunRPC]
	public void NetworkMatchOver(int id)
	{
		_winnerUI.ShowWinner(_scoreUI.GetUserNameFromID(id));
	}

	[PunRPC]
	void NetworkStartGame(double delta)
	{
		// init master class that have last say in all collisions(will only be called on the server)
		FindObjectOfType<CollisionTracker>().ManualStart();

		// create level (player creation is here for now aswell)
		_level.StartGameOnline();

		// start countdown
		_counterUI.StartCount(delta, 3, () => matchStarted = true);
	}
	
	[PunRPC]
	void NetworkStartNewRound(double delta)
	{		
		matchStarted = false;

		_currentGameMode.OnRoundRestarted();

		// reset level(character resapwn is here aswell for now)
		_level.ResetRound();

		// update score ui and restart timer
		_counterUI.StartCount(delta, 3, () => matchStarted = true);
	}

	[PunRPC]
	public void RegisterPlayer(int id, string nickName, string viewName)
	{
		// register a player by id for scorekepping and ui
		_currentGameMode.OnPlayerRegistred(id);
		_scoreUI.RegisterPlayer(id, nickName, viewName);
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

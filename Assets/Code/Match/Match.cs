using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum GameMode
{
	KingOfTheHill,
	TurfWar,
	UltimateKiller,
}

public class Match : Photon.MonoBehaviour
{
	// TEMP FOR SETTING GAMEMODE FOR LOCAL PLAY
	[SerializeField] GameMode _debugCurrentGameMode;

	public static Match instance       { get; private set; }
	public bool matchStarted           { get; private set; }
	public CameraController gameCamera { get; private set; }

	[Header("GAME REFERENCES")]
	[SerializeField] Level          _level;         public Level level { get { return _level; } }
	[SerializeField] GameModesModel _gameModeModel; public GameModesModel gameModeModel { get { return _gameModeModel; } }

	public ScoreUI        scoreUI        { get; private set; }
	public StartCounterUI counterUI      { get; private set; }
	public WinnerUI       winnerUI       { get; private set; }
	public MessagePromt   msgPromt       { get; private set; }
	public StartCounterUI roundCounterUI { get; private set; }

	SoundManager _musicManager;

	public GameMode currentGameModeType { get; private set; }
	IGameMode _currentGameMode;
	
	void Awake()
	{
		instance = this;

		gameCamera = FindObjectOfType<CameraController>();
	}

	void Start()
	{
		InGameUI UI    = InGameUI.instance;
		scoreUI        = UI.scoreUI;
		counterUI      = UI.startCounterUI;
		winnerUI       = UI.winnerUI;
		msgPromt       = UI.msgPromt;
		roundCounterUI = UI.roundCounter;

		_musicManager = SoundManager.instance;

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

		if      (currentGameModeType == GameMode.KingOfTheHill)  _currentGameMode = GetComponent<GameModeKingOfTheHill>();
		else if (currentGameModeType == GameMode.TurfWar)        _currentGameMode = GetComponent<GameModeTurfWar>();
		else if (currentGameModeType == GameMode.UltimateKiller) _currentGameMode = GetComponent<GameModeUltimateKiller>();

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		scoreUI.Setup(numPlayer, currentGameModeType);

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

		// TEMP STUFF UNTILL LOCAL PLAY MENUS HAVE BEEN CREATED
		currentGameModeType = _debugCurrentGameMode;

		if      (currentGameModeType == GameMode.KingOfTheHill)  _currentGameMode = GetComponent<GameModeKingOfTheHill>();
		else if (currentGameModeType == GameMode.TurfWar)        _currentGameMode = GetComponent<GameModeTurfWar>();
		else if (currentGameModeType == GameMode.UltimateKiller) _currentGameMode = GetComponent<GameModeUltimateKiller>();

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		scoreUI.Setup(numPlayer, _debugCurrentGameMode);

		_level.StartGameLocal();

		_currentGameMode.OnLevelCreated();

		counterUI.StartCount(0, 3, () => { matchStarted = true; _currentGameMode.OnRoundStart(); });
	}

	// called from character when it is created
	[PunRPC]
	public void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName)
	{
		// register a player by id for scorekepping and ui
		_currentGameMode.OnPlayerRegistred(playerPhotonID);
		scoreUI.RegisterPlayer(playerPhotonID, playerIndexID, nickName, viewName);
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

		_currentGameMode.OnLevelCreated();

		// start countdown
		counterUI.StartCount(delta, 3, () => { matchStarted = true; _currentGameMode.OnRoundStart(); });
	}

	// called from character(only on server in online play) 
	public void OnPlayerDie(int killedPlayerID, int killerPLayerID)
	{
		_currentGameMode.OnPlayerDie(killedPlayerID, killerPLayerID);
	}

	// called on all clients from master client
	// when a player is changing the color of a tile
	public void OnTileChangingColor(int oldPlayerPhotonID, int newPlayerPhotonID)
	{
		// only change score etc if the current mode is turf war
		// we can technicly have changing tile colors even if we
		// are playing another gamemode, if that would be the case
		// only this part is ignored
		if (currentGameModeType == GameMode.TurfWar)
		{
			GameModeTurfWar turfWar = (GameModeTurfWar)_currentGameMode;

			// only remove score if the tile have been occupied before
			if (oldPlayerPhotonID != Constants.INVALID_ID)			
				turfWar.RemoveTileScoreFrom(oldPlayerPhotonID);

			turfWar.AddTileScoreTo(newPlayerPhotonID);			
		}
	}
	
	// called from gamemode (called on all clients)
	// just used for setting UI and setting feedback
	public void OnRoundOver(int winnerId, int score)
	{
		_musicManager.StopSharedPowerUpLoop(0.5f);
		scoreUI.UpdateRoundScore(winnerId, score);
		matchStarted = false;
	}
	
	//called on all clients and then everyone will start next round count
	[PunRPC]
	public void NetworkSetEndRoundDelay(double delay, double delta)
	{				
		// do small delay before we reset to new round
		Timing.RunCoroutine(_resetDelay(delay, delta));		
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
		scoreUI.DisableUIOfDisconnectedPlayer(id);
	}

	// called from gamemode to all clients
	[PunRPC]
	public void NetworkMatchOver(int id)
	{
		winnerUI.ShowWinner(scoreUI.GetUserNameFromPhotonID(id));
	}
	
	// called from the coroutine that handle delay before next round should start
	void StartNextRound(double delta)
	{		
		matchStarted = false;

		_currentGameMode.OnRoundRestarted();

		// reset level(character resapwn is here aswell for now)
		_level.ResetRound();

		scoreUI.ClearRoundUI();

		// restart start timer
		counterUI.StartCount(delta, 3, () => { matchStarted = true; _currentGameMode.OnRoundStart(); });
	}	

	// this runs locally on all clients with the net delta
	// all clients then start the next round counter locally
	IEnumerator<float> _resetDelay(double delay, double delta)
	{
		if (Constants.onlineGame)
			delay -= (PhotonNetwork.time - delta);

		while (delay > 0)
		{
			delay -= Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}

		if (Constants.onlineGame)
			StartNextRound(PhotonNetwork.time);

		if (!Constants.onlineGame)
			StartNextRound(0);
	}

	void ShowLastPlayerMessage()
	{
		matchStarted = false;
		msgPromt.SetAndShow("All Players have left the room!\n Returning to menu!", () =>
		{
			PhotonHelpers.ClearPlayerProperties(PhotonNetwork.player);
			PhotonNetwork.LeaveRoom();
			MainMenuSystem.reclaimPlayerUI = false;
			MainMenuSystem.startPage = MenuPageType.StartScreen;
			Timing.KillCoroutines();
			UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
		});

	}
}

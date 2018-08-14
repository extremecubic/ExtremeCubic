using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class Match : Photon.MonoBehaviour
{
	public static Match instance { get; private set; }
	public bool matchStarted     { get; private set; }

	[Header("GAME REFERENCES")]
	[SerializeField] MusicManager _musicManager; public MusicManager musicManager { get { return _musicManager; } }
	[SerializeField] Level        _level;        public Level        level        { get { return _level; } }

	[Header("UI REFERENCES")]
	[SerializeField] ScoreUI        _scoreUI;
	[SerializeField] StartCounterUI _counterUI;
	[SerializeField] WinnerUI       _winnerUI;

	IGameMode _currentGameMode;
	
	void Awake()
	{
		instance = this;

		// only have one gamemode for now
		_currentGameMode = GetComponent<GameModeLastMan>();
	}

	void Start()
	{		
		// check if we start from menu or the simple network starter directly from level scen
		if (FindObjectOfType<SimpleNetworkStarter>() == null)
		{
			SetupMatch();
			StartOnAllLoaded();
		}
	}

	// only used directly from starting game from levelscene
	public void SimpleStart()
	{
		SetupMatch();
		NetworkStartGame(PhotonNetwork.time);
	}

	void OnDestroy()
	{
		instance = null;	
	}

	void SetupMatch()
	{
		// init data structures
		int numPlayer = PhotonNetwork.room.PlayerCount;

		_currentGameMode.OnSetup(numPlayer);

		// tell the ui how many players we are
		_scoreUI.Setup(numPlayer);
	}

	void StartOnAllLoaded()
	{
		// this rpc will execute when all players are loaded in the scene
		// the masterclient will then send a message with timestamp to all clients to start the countdown
		if (PhotonNetwork.isMasterClient)
			photonView.RPC("AllIsLoaded", PhotonTargets.MasterClient);
	}

	// called from character(only on server) 
	public void OnPlayerDie(int playerId, int viewID)
	{
		_currentGameMode.OnPlayerDie(playerId, viewID);
	}
	
	// called from gamemode (called on all clients)
	public void OnRoundOver(int winnerId, int score)
	{
		musicManager.StopSharedPowerUpLoop(0.5f);
		_scoreUI.UpdateScore(winnerId, score);
	}
	
	public void SetCoundownToRoundRestart(float delay)
	{				
		// do small delay before we reset to new round
		Timing.RunCoroutine(_resetDelay(delay));		
	}
	
	// callback from when countdown is done
	// matchStarted will enable input for the players
	public void OnCounterZero()
	{
		matchStarted = true;
	}

	// callback from character when someone disconnects, called locally on all clients
	public void OnPlayerLeft(int id)
	{
		_currentGameMode.OnPlayerLeft(id);
		_scoreUI.DisableUIOfDisconnectedPlayer(id);
	}

	[PunRPC]
	void NetworkMatchOver(int id)
	{
		_winnerUI.ShowWinner(id);
	}

	[PunRPC]
	void AllIsLoaded()
	{
		// tell everyone to start the match, send the timestamp so the countdown timer will be the same for all players no matter lag
		photonView.RPC("NetworkStartGame", PhotonTargets.All, PhotonNetwork.time);
	}

	[PunRPC]
	void NetworkStartGame(double delta)
	{
		// init master class that have last say in all collisions(will only be called on the server)
		FindObjectOfType<CollisionTracker>().ManualStart();

		// create level (player creation is here for now aswell)
		_level.ManualStart();

		// start countdown
		_counterUI.StartCount(delta, 3, () => OnCounterZero());
	}
	
	[PunRPC]
	void NetworkStartNewRound(double delta)
	{		
		matchStarted = false;

		_currentGameMode.OnRoundRestarted();

		// reset level(character resapwn is here aswell for now)
		_level.ResetRound();

		// update score ui and restart timer
		_counterUI.StartCount(delta, 3, () => OnCounterZero());
	}

	[PunRPC]
	void RegisterPlayer(int id, string name)
	{
		// register a player by id for scorekepping and ui
		_currentGameMode.OnPlayerRegistred(id);
		_scoreUI.RegisterPlayer(id, name);
	}

	IEnumerator<float> _resetDelay(float delay)
	{
		float timer = 0;
		while (timer < delay)
		{
			timer += Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}

		photonView.RPC("NetworkStartNewRound", PhotonTargets.All, PhotonNetwork.time);
	}
}

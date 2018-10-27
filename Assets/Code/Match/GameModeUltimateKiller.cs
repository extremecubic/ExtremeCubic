using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class GameModeUltimateKiller : Photon.MonoBehaviour, IGameMode
{
	public class UltimateKillerPlayerTracker
	{
		public int roundScore;
		public int killScore;
		public bool disconnected;
	}

	Dictionary<int, UltimateKillerPlayerTracker> _players;
	Dictionary<Vector2DInt, string>              _destroyedTiles;	
	List<Tile>                                   _respawnedTiles;

	Match           _match;
	GameModesModel  _modeModel;
	CoroutineHandle _tileRespawnHandle;

	int _numBreakableTiles;
	int _numDestroyedTilesForRespawn;

	void Awake()
	{
		_match     = GetComponent<Match>();
		_modeModel = _match.gameModeModel;
	}

	public void OnSetup(int numPlayers)
	{
		_players            = new Dictionary<int, UltimateKillerPlayerTracker>();
		_destroyedTiles     = new Dictionary<Vector2DInt, string>();
		_respawnedTiles = new List<Tile>();
	}

	public void OnLevelCreated()
	{
		TileMap TM = _match.level.tileMap;

		// get how many tiles is breakable in the current tilemap and
		// calculate after how many we want to respawn the tiles
		_numBreakableTiles           = TM.GetNumBreakableTiles();
		_numDestroyedTilesForRespawn = (int)(_numBreakableTiles * _modeModel.tilePercentDestroyedForRespawn);

		// this will notify us each time a tile
		// have been destroyed and will give us the 
		// net delta when it got destroyed on server
		TM.OnTileDestroyed += AddDestroyedTile;
	}

	// only called on server
	public void OnPlayerDie(int killedPlayerID, int killerID)
	{
		if (Constants.onlineGame)
			photonView.RPC("UltimateKillerNetworkPlayerDied", PhotonTargets.All, killedPlayerID, killerID, PhotonNetwork.time);

		if (!Constants.onlineGame)
			UltimateKillerNetworkPlayerDied(killedPlayerID, killerID, 0.0);
	}

	// add the coords and tiletype of the destroyed tile
	public void AddDestroyedTile (string tileType, Vector2DInt coords, double netDelta)
	{
		if (!_destroyedTiles.ContainsKey(coords))
		{
			_destroyedTiles.Add(coords, tileType);

			// if enough tiles have been destroyed 
			// we need to respawn them
			if (_destroyedTiles.Count >= _numDestroyedTilesForRespawn)
				RespawnDestroyedTiles(netDelta);

			return;
		}

		// a tile can be destroyed and replaced by another tile in 
		// some instances, if the replacing tile have been destroyed
		// aswell we just replace the type
		_destroyedTiles[coords] = tileType;
	}

	public void OnPlayerLeft(int ID)
	{
		_players[ID].disconnected = true;
	}

	public void OnPlayerRegistred(int ID)
	{
		_players.Add(ID, new UltimateKillerPlayerTracker());
	}

	public void OnRoundRestarted()
	{
		// reset tile score
		foreach (var p in _players)
			p.Value.killScore = 0;

		_tileRespawnHandle.IsRunning = false;

		_destroyedTiles.Clear();
		_respawnedTiles.Clear();
		_respawnedTiles.Clear();
	}

	public void OnRoundStart()
	{
		// all clients will keep track of roundtime in case
		// of server migration
		Timing.RunCoroutine(_RoundDuration());

		if (Constants.onlineGame)
			_match.roundCounterUI.StartCount(PhotonNetwork.time, _modeModel.killerRoundTime, null);

		if (!Constants.onlineGame)
			_match.roundCounterUI.StartCount(0, _modeModel.killerRoundTime, null);
	}

	// will start the UI showing the respawn time on all clients
	// and will either give a point to the player that killed the player that died
	// or will take one point away from the killed player if suicide
	[PunRPC]
	void UltimateKillerNetworkPlayerDied(int killedPlayerID, int killerID, double delta)
	{
		if (killerID == Constants.INVALID_ID)
		{
			_players[killedPlayerID].killScore--;
			_match.scoreUI.UpdateKillsScore(killedPlayerID, _players[killedPlayerID].killScore);
		}
		else
		{
			_players[killerID].killScore++;
			_match.scoreUI.UpdateKillsScore(killerID, _players[killerID].killScore);
		}

		_match.scoreUI.SetRespawnUI(killedPlayerID, delta);
	}

	IEnumerator<float> _RoundDuration()
	{
		yield return Timing.WaitForSeconds((float)_modeModel.killerRoundTime);

		// only the master client will calculate the winner and
		// then send the info to all other clients
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			SetWinner();

		if (!Constants.onlineGame)
			SetWinner();
	}

	void SetWinner()
	{
		int winnerID = 0;
		int highestKillScore = -999;

		// get the ID of the player that had the most turf tiles of this round
		// TODO: this is not handeling draws for the moment
		foreach (var p in _players)
		{
			if (!p.Value.disconnected)
			{
				if (p.Value.killScore > highestKillScore)
				{
					winnerID = p.Key;
					highestKillScore = p.Value.killScore;
				}
			}
		}

		// call rpc so all clients can incrase the score of the winning player
		if (Constants.onlineGame)
			photonView.RPC("UltimateKillerNetworkRoundOver", PhotonTargets.All, winnerID);

		if (!Constants.onlineGame)
			UltimateKillerNetworkRoundOver(winnerID);

		// check if the match is over or if we should start next round		
		if (_players[winnerID].roundScore == _modeModel.killerNumRoundsToWin)
		{
			if (Constants.onlineGame)
				_match.photonView.RPC("NetworkMatchOver", PhotonTargets.All, winnerID);

			if (!Constants.onlineGame)
				_match.NetworkMatchOver(winnerID);
		}
		else
		{
			// start delay before the next round countdown will start 
			// the net delta is sent to sync the start of next round on all clients
			if (Constants.onlineGame)
				_match.photonView.RPC("NetworkSetEndRoundDelay", PhotonTargets.All, 2.0, PhotonNetwork.time);

			if (!Constants.onlineGame)
				_match.NetworkSetEndRoundDelay(2.0, 0.0);
		}
	}

	// all clients keep track of score in case of server migration
	// tell match.cs witch player that won so the score UI can be updated
	[PunRPC]
	void UltimateKillerNetworkRoundOver(int winnerID)
	{
		_players[winnerID].roundScore++;
		_match.OnRoundOver(winnerID, _players[winnerID].roundScore);
	}

	void RespawnDestroyedTiles(double netDelta)
	{
		_tileRespawnHandle = Timing.RunCoroutineSingleton(_RespawnTiles(netDelta), _tileRespawnHandle, SingletonBehavior.Abort);
	}

	IEnumerator<float> _RespawnTiles(double netDelta)
	{
		TileMap TM = _match.level.tileMap;

		// create all the tiles that have been destroyed
		// dont add them to the tile map yet becuase we want to
		// move the view odf the tile into position first
		foreach (var destroyedTile in _destroyedTiles)		
			_respawnedTiles.Add(new Tile(destroyedTile.Key, destroyedTile.Value, 0.0f, 1.0f, TM.tilesFolder));

		// get movement settings for respawn tiles
		double moveUpForSeconds = _modeModel.tilesRespawnMoveDuration; 
		float  startDepth       = _modeModel.tilesRespawnStartDepth;

		// remove the net delta from duration if online game
		if (Constants.onlineGame)
			moveUpForSeconds -= (PhotonNetwork.time - netDelta);

		// initialize variables that we need
		float   fraction          = 0.0f;
		float   currentDepth      = 0.0f;
		Vector3 position          = Vector3.zero;

		while (moveUpForSeconds > 0)
		{
			moveUpForSeconds -= Time.deltaTime;

			// get the current depth position of tile based
			// on the fraction of timer and the start depth
			fraction     = Mathf.InverseLerp(5.0f, 0.0f, (float)moveUpForSeconds);
			currentDepth = Mathf.Lerp(startDepth, 0, fraction);

			// set the position of each tile
			foreach (Tile tile in _respawnedTiles)
			{
				position = tile.view.transform.position;
				position.y = currentDepth;
				tile.view.transform.position = position;
			}

			yield return Timing.WaitForOneFrame;
		}

		// will set the tiles in the tilemap 
		// so they are walkable again
		foreach (Tile tile in _respawnedTiles)
			TM.SetTile(tile.position, tile, 1.0f, 0.0, false);

		_destroyedTiles.Clear();
		_respawnedTiles.Clear();
		_respawnedTiles.Clear();
	}

}

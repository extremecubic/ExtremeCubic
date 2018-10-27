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

	Match          _match;
	GameModesModel _modeModel;

	void Awake()
	{
		_match     = GetComponent<Match>();
		_modeModel = _match.gameModeModel;
	}

	public void OnSetup(int numPlayers)
	{
		_players = new Dictionary<int, UltimateKillerPlayerTracker>();
	}

	// only called on server
	public void OnPlayerDie(int killedPlayerID, int killerID)
	{
		if (Constants.onlineGame)
			photonView.RPC("UltimateKillerNetworkPlayerDied", PhotonTargets.All, killedPlayerID, killerID, PhotonNetwork.time);

		if (!Constants.onlineGame)
			UltimateKillerNetworkPlayerDied(killedPlayerID, killerID, 0.0);
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

}

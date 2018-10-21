using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class GameModeTurfWar : Photon.MonoBehaviour, IGameMode
{
	public class TurfWarPlayerTracker
	{
		public int roundScore;
		public int turfScore;
		public bool disconnected;
	}

	Dictionary<int, TurfWarPlayerTracker> _players;
	Match _match;
	GameModesModel _modeModel;

	void Awake()
	{
		_match     = GetComponent<Match>();
		_modeModel = _match.gameModeModel;
	}

	public void OnSetup(int numPlayers)
	{
		_players = new Dictionary<int, TurfWarPlayerTracker>();
	}

	// remove a point from the player that had this turf before
	public void RemoveTileScoreFrom(int playerPhotonID)
	{
		_players[playerPhotonID].turfScore--;
		_match.scoreUI.UpdateTurfScore(playerPhotonID, _players[playerPhotonID].turfScore);
	}

	// add a point to the player that took over this turf
	public void AddTileScoreTo(int playerPhotonID)
	{
		_players[playerPhotonID].turfScore++;
		_match.scoreUI.UpdateTurfScore(playerPhotonID, _players[playerPhotonID].turfScore);
	}

	// send rpc to all clients to update the UI with the 
	// time untill player will respawn
	public void OnPlayerDie(int ID)
	{
		if (Constants.onlineGame)
			photonView.RPC("TurfWarNetworkPlayerDied", PhotonTargets.All, ID, PhotonNetwork.time);

		if (!Constants.onlineGame)
			TurfWarNetworkPlayerDied(ID, 0.0);
	}

	// set player to disconected so we wont take this
	// players score in to acount
	public void OnPlayerLeft(int ID)
	{
		_players[ID].disconnected = true;
	}

	public void OnPlayerRegistred(int ID)
	{
		_players.Add(ID, new TurfWarPlayerTracker());
	}

	// called locally on all clients from a delayConter in Match.cs
	// this delay counter have already taken the netdelta into acount
	// so this call will happen at the same time on all clients
	public void OnRoundStart()
	{
		// all clients will keep track of roundtime in case
		// of server migration
		Timing.RunCoroutine(_RoundDuration());

		if (Constants.onlineGame)
		    _match.roundCounterUI.StartCount(PhotonNetwork.time, _modeModel.turfRoundTime, null);

		if (!Constants.onlineGame)
			_match.roundCounterUI.StartCount(0, _modeModel.turfRoundTime, null);
	}

	public void OnRoundRestarted()
	{
		// reset tile score
		foreach (var p in _players)
			p.Value.turfScore = 0;
	}

	IEnumerator<float> _RoundDuration()
	{
		yield return Timing.WaitForSeconds((float)_modeModel.turfRoundTime);

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
		int highestTurfScore = 0;

		// get the ID of the player that had the most turf tiles of this round
		// TODO: this is not handeling draws for the moment
		foreach (var p in _players)
		{
			if (!p.Value.disconnected)
			{
				if (p.Value.turfScore > highestTurfScore)
				{
					winnerID = p.Key;
					highestTurfScore = p.Value.turfScore;
				}
			}
		}

		// call rpc so all clients can incrase the score of the winning player
		if (Constants.onlineGame)
			photonView.RPC("TurfWarNetworkRoundOver", PhotonTargets.All, winnerID);

		if (!Constants.onlineGame)
			TurfWarNetworkRoundOver(winnerID);

		// check if the match is over or if we should start next round		
		if (_players[winnerID].roundScore == _modeModel.turfNumRoundsToWin)
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
	void TurfWarNetworkRoundOver(int winnerID)
	{
		_players[winnerID].roundScore++;
		_match.OnRoundOver(winnerID, _players[winnerID].roundScore);
	}

	// will start the Ui showing the respawn time on all clients
	[PunRPC]
	void TurfWarNetworkPlayerDied(int playerID, double delta)
	{
		_match.scoreUI.SetRespawnUI(playerID, delta, GameMode.TurfWar);
	}
}

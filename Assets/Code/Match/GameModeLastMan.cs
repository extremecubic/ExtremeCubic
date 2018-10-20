using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeLastMan : Photon.MonoBehaviour, IGameMode
{
	public class LastManPlayerTracker
	{
		public int score;
		public bool dead;
		public bool disconnected;
	}

	Dictionary<int, LastManPlayerTracker> _players;

	int   _numPlayers;
	bool  _winnerSet;
	Match _match;
	GameModesModel _modeModel;

	void Awake()
	{
		_match = GetComponent<Match>();
		_modeModel = _match.gameModeModel;
	}

	public void OnSetup(int numPlayers)
	{
		_players = new Dictionary<int, LastManPlayerTracker>();
	}

	public void OnPlayerLeft(int ID)
	{
		_players[ID].disconnected = true;

		// need to check if we automaticly got a winner when a player left the room
		if (PhotonNetwork.isMasterClient)
			OnPlayerDie(ID);
	}

	public void OnPlayerRegistred(int ID)
	{
		_players.Add(ID, new LastManPlayerTracker());
	}

	public void OnPlayerDie(int playerId)
	{
		if (_winnerSet) // if last player dies after winning we dont want to do nothing
			return;

		_players[playerId].dead = true;

		int numAlive = 0;
		int idLastAlive = 0;

		// get how many players is left alive
		foreach (var p in _players)
			if (!p.Value.dead && !p.Value.disconnected)
			{
				numAlive++;
				idLastAlive = p.Key;
			}

		// round over
		if (numAlive <= 1)
		{
			RoundOver(idLastAlive);
			return;
		}

		// send who died to others in case of server migration
		if (Constants.onlineGame)
			photonView.RPC("NetworkPlayerDied", PhotonTargets.Others, playerId);
	}

	void RoundOver(int winnerId)
	{
		_winnerSet = true;

		if (Constants.onlineGame)
	        photonView.RPC("LastManNetworkRoundOver", PhotonTargets.All, winnerId);

		if (!Constants.onlineGame)
			LastManNetworkRoundOver(winnerId);

		// check if the match is over or if we should start next round		
		if (_players[winnerId].score == _modeModel.kingNumRoundsToWin)
		{
			if (Constants.onlineGame)
				_match.photonView.RPC("NetworkMatchOver", PhotonTargets.All, winnerId);

			if (!Constants.onlineGame)
				_match.NetworkMatchOver(winnerId);
		}
		else
		{
			if (Constants.onlineGame)
				_match.photonView.RPC("NetworkSetEndRoundDelay", PhotonTargets.All, 2.0, PhotonNetwork.time);

			if (!Constants.onlineGame)
				_match.NetworkSetEndRoundDelay(2.0, 0.0);
		}
	}

	public void OnRoundStart()
	{
	}

	public void OnRoundRestarted()
	{
		_winnerSet = false;

		// untag dead players
		foreach (var p in _players)
			p.Value.dead = false;
	}
	
	[PunRPC]
	void LastManNetworkRoundOver(int winnerID)
	{
		// make clients keep track of score aswell in case of server migration
		_players[winnerID].score++;

		// increment score and tell match to update UI
		_match.OnRoundOver(winnerID, _players[winnerID].score);
	}

	[PunRPC]
	void NetworkPlayerDied(int playerId)
	{
		_players[playerId].dead = true;
	}

}

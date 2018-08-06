using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeLastMan : Photon.MonoBehaviour, IGameMode
{
	const int _numRoundsToWin = 3;

	public class PlayerTracker
	{
		public int score;
		public bool dead;
		public bool disconnected;
	}

	Dictionary<int, PlayerTracker> _players;

	int _numPlayers;
	bool _winnerSet;
	Match _match;

	void Awake()
	{
		_match = GetComponent<Match>();
	}

	public void OnSetup(int numPlayers)
	{
		_players = new Dictionary<int, PlayerTracker>();
	}

	public void OnPlayerLeft(int ID)
	{
		_players[ID].disconnected = true;
	}

	public void OnPlayerRegistred(int ID)
	{
		_players.Add(ID, new PlayerTracker());
	}

	public void OnPlayerDie(int playerId, int viewID)
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

		// if all players but 1 is disconnected just give the point to this player(match should be cancelled but keep this for now to avoid nullrefs)
		if (PhotonNetwork.room.PlayerCount == 1)
			idLastAlive = PhotonNetwork.playerList[0].ID;

		// round over
		if (numAlive <= 1)
			RoundOver(idLastAlive);
	}

	void RoundOver(int winnerId)
	{
		_winnerSet = true;

		photonView.RPC("NetworkRoundOver", PhotonTargets.All, winnerId);

		//check if the match is over or if we should start next round		
		if (_players[winnerId].score == _numRoundsToWin)
			_match.photonView.RPC("NetworkMatchOver", PhotonTargets.All, winnerId);
		else
			_match.SetCoundownToRoundRestart(2.0f);
	}

	public void OnRoundRestarted()
	{
		_winnerSet = false;

		// untag dead players
		foreach (var p in _players)
			p.Value.dead = false;
	}
	
	[PunRPC]
	void NetworkRoundOver(int winnerID)
	{
		// make clients keep track of score aswell in case of server migration
		_players[winnerID].score++;

		// increment score and tell match to update UI
		_match.OnRoundOver(winnerID, _players[winnerID].score);
	}

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class GameModeTurfWar : Photon.MonoBehaviour, IGameMode
{
	const int _numRoundsToWin = 3;
	const float _roundlength = 20.0f;

	public class TurfWarPlayerTracker
	{
		public int roundScore;
		public int tileScore;
		public bool disconnected;
	}

	Dictionary<int, TurfWarPlayerTracker> _players;
	Match _match;

	void Awake()
	{
		_match = GetComponent<Match>();
	}

	public void OnSetup(int numPlayers)
	{
		_players = new Dictionary<int, TurfWarPlayerTracker>();
	}

	public void OnPlayerDie(int ID)
	{
		
	}

	public void OnPlayerLeft(int ID)
	{
		_players[ID].disconnected = true;
	}

	public void OnPlayerRegistred(int ID)
	{
		_players.Add(ID, new TurfWarPlayerTracker());
	}

	public void OnRoundStart()
	{
		Timing.RunCoroutine(_RoundDuration());
	}

	public void OnRoundRestarted()
	{
		// reset tile score
		foreach (var p in _players)
			p.Value.tileScore = 0;
	}

	IEnumerator<float> _RoundDuration()
	{
		yield return Timing.WaitForSeconds(_roundlength);

		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			SetWinner();

		if (!Constants.onlineGame)
			SetWinner();
	}

	void SetWinner()
	{
		int winnerID = 0;

		foreach (var p in _players)
		{
			winnerID = p.Key;			
			break;
		}

		if (Constants.onlineGame)
			photonView.RPC("TurfWarNetworkRoundOver", PhotonTargets.All, winnerID);

		if (!Constants.onlineGame)
			TurfWarNetworkRoundOver(winnerID);

		// check if the match is over or if we should start next round		
		if (_players[winnerID].roundScore == _numRoundsToWin)
		{
			if (Constants.onlineGame)
				_match.photonView.RPC("NetworkMatchOver", PhotonTargets.All, winnerID);

			if (!Constants.onlineGame)
				_match.NetworkMatchOver(winnerID);
		}
		else
			_match.SetCoundownToRoundRestart(2.0f);
	}

	[PunRPC]
	void TurfWarNetworkRoundOver(int winnerID)
	{
		_players[winnerID].roundScore++;
		_match.OnRoundOver(winnerID, _players[winnerID].roundScore);
	}

}

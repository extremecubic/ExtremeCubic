using UnityEngine;

public class ScoreUI : MonoBehaviour
{
	[SerializeField] PlayerKingOfTheHillUI[] _kingOfTheHillUI;
	[SerializeField] PlayerTurfWarUI[]       _turfWarUI;
	
	int              _numPlayers;	
	PlayerUIItem[][] _playerUI;

	GameMode _gameMode;
	int      _modeIndex;

	// Setup all UI for the different gamemodes into the generic
	// PlayerUIItem array and enable the UI for how many players we are
	public void Setup(int numPlayers, GameMode mode)
	{
		_numPlayers = numPlayers;
		_gameMode   = mode;
		_modeIndex  = (int)mode;

		_playerUI = new PlayerUIItem[2][];

		_playerUI[0] = _kingOfTheHillUI;
		_playerUI[1] = _turfWarUI;

		for (int i = 0; i < _numPlayers; i++)
			_playerUI[_modeIndex][i].EnableUI(true);		
	}

	// return the username that is saved in the UI item
	// with the specific photon ID
	public string GetUserNameFromPhotonID(int playerPhotonID)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (_playerUI[_modeIndex][i].ownerID == playerPhotonID)
				return _playerUI[_modeIndex][i].GetUserName();

		return "";
	}

	// check for first available UI item in array
	// and set it up with the properties of this player
	public void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (!_playerUI[_modeIndex][i].taken)
			{
				_playerUI[_modeIndex][i].RegisterPlayer(playerPhotonID, playerIndexID, nickName, viewName);
				return;
			}
	}

	// update the UI score of the winning player of this round
	public void UpdateRoundScore(int playerPhotonID, int score)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (_playerUI[_modeIndex][i].ownerID == playerPhotonID)
			{
				_playerUI[_modeIndex][i].UpdateRoundScore(score);
				return;
			}
	}

	// uppdate the turf score of the player with the specified photonID
	public void UpdateTurfScore(int playerPhotonID, int newScore)
	{
		for (int i =0; i < _numPlayers; i++)
			if (_playerUI[_modeIndex][i].ownerID == playerPhotonID)
			{
				PlayerTurfWarUI player = (PlayerTurfWarUI)_playerUI[_modeIndex][i];
				player.UpdateTurfScore(newScore);
				return;
			}
	}

	// disable the UI item of a player that left the room
	public void DisableUIOfDisconnectedPlayer(int playerPhotonID)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (_playerUI[_modeIndex][i].ownerID == playerPhotonID)
			{
				_playerUI[_modeIndex][i].EnableUI(false);
				return;
			}
	}

	// clear all Round Specific UI
	public void ClearRoundUI()
	{
		for (int i = 0; i < _numPlayers; i++)
			_playerUI[_modeIndex][i].ClearRoundUI();			
	}

	// set and handle the UI for respawning
	public void SetRespawnUI(int playerPhotonID, double delta)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (_playerUI[_modeIndex][i].ownerID == playerPhotonID)
			{
				_playerUI[_modeIndex][i].SetRespawnUI(delta, _gameMode);
				return;
			}
	}
	
	
}

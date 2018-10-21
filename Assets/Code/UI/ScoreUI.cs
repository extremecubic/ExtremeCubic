using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using MEC;

// THIS FILE SUCKS
// I COULD NOT THINK OF A BETTER WAY OF HANDELING
// THE IN GAME UI FOR THE DIFFERENT GAME MODES WITHOUT MAKING IT
// REALLY BAD TO WORK WITH FROM THE INSPECTOR, TRY TO COME UP WITH A WAY OF
// GENERALISING HOW WE HANDLE THIS 
public class ScoreUI : MonoBehaviour
{
	[Serializable]
	public struct PlayerElementKingOfHill
	{
		public GameObject content;
		public Text scoreText;
		public Text userName;
		public Image icon;

		[NonSerialized] public int ownerID;
		[NonSerialized] public bool taken;
	}

	[Serializable]
	public struct PlayerElementTurfWar
	{
		public GameObject content;
		public Text scoreText;
		public Text userName;
		public Image icon;
		public GameObject respawnParent;
		public Text respawnTimeText;
		public Text tilesText;
		public Image colorImage;

		[NonSerialized] public int ownerID;
		[NonSerialized] public bool taken;
		[NonSerialized] public CoroutineHandle respawnHandle;
	}

	int _numPlayers;
	[SerializeField] PlayerElementKingOfHill[] _playersKingOfHill;
	[SerializeField] PlayerElementTurfWar[]    _playersTurfWar;

	public void Setup(int numPlayers, GameMode mode)
	{
		_numPlayers = numPlayers;

		if (mode == GameMode.KingOfTheHill)
		{
			for (int i = 0; i < numPlayers; i++)
				_playersKingOfHill[i].content.SetActive(true);
		}
		else if(mode == GameMode.TurfWar)
		{
			for (int i = 0; i < numPlayers; i++)
				_playersTurfWar[i].content.SetActive(true);
		}
	}

	public string GetUserNameFromPhotonID(int playerPhotonID, GameMode mode)
	{
		if (mode == GameMode.KingOfTheHill)
		{
			for (int i = 0; i < _numPlayers; i++)
				if (_playersKingOfHill[i].ownerID == playerPhotonID)
					return _playersKingOfHill[i].userName.text;
		}
		else if (mode == GameMode.TurfWar)
		{
			for (int i = 0; i < _numPlayers; i++)
				if (_playersTurfWar[i].ownerID == playerPhotonID)
					return _playersTurfWar[i].userName.text;
		}

		return "";
	}

	public void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName, GameMode mode)
	{
		if (mode == GameMode.KingOfTheHill)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (!_playersKingOfHill[i].taken)
				{
					_playersKingOfHill[i].ownerID = playerPhotonID;
					_playersKingOfHill[i].taken = true;
					_playersKingOfHill[i].userName.text = nickName;
					_playersKingOfHill[i].icon.sprite = CharacterDatabase.instance.GetViewFromName(viewName).iconUI;
					_playersKingOfHill[i].scoreText.text = "0";
					return;
				}
			}
		}
		else if (mode == GameMode.TurfWar)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (!_playersTurfWar[i].taken)
				{
					_playersTurfWar[i].ownerID = playerPhotonID;
					_playersTurfWar[i].taken = true;
					_playersTurfWar[i].userName.text = nickName;
					_playersTurfWar[i].icon.sprite = CharacterDatabase.instance.GetViewFromName(viewName).iconUI;
					_playersTurfWar[i].scoreText.text = "0";
					_playersTurfWar[i].tilesText.text = "0";
					_playersTurfWar[i].respawnParent.SetActive(false);
					_playersTurfWar[i].colorImage.color = Match.instance.gameModeModel.GetColorFromPlayerIndexID(playerIndexID);
					return;
				}
			}
		}
	}

	public void UpdateRoundScore(int playerPhotonID, int score, GameMode mode)
	{
		if (mode == GameMode.KingOfTheHill)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (_playersKingOfHill[i].ownerID == playerPhotonID)
				{
					_playersKingOfHill[i].scoreText.text = score.ToString();
					return;
				}
			}
		}
		else if (mode == GameMode.TurfWar)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (_playersTurfWar[i].ownerID == playerPhotonID)
				{
					_playersTurfWar[i].scoreText.text = score.ToString();
					return;
				}
			}
		}		
	}

	public void UpdateTurfScore(int playerPhotonID, int newScore)
	{
		for (int i = 0; i < _numPlayers; i++)
		{
			if (_playersTurfWar[i].ownerID == playerPhotonID)
			{
				_playersTurfWar[i].tilesText.text = newScore.ToString();
				return;
			}
		}
	}

	public void DisableUIOfDisconnectedPlayer(int playerPhotonID, GameMode mode)
	{
		if (mode == GameMode.KingOfTheHill)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (_playersKingOfHill[i].ownerID == playerPhotonID)
				{
					_playersKingOfHill[i].content.SetActive(false);
					return;
				}
			}
		}
		else if (mode == GameMode.TurfWar)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (_playersTurfWar[i].ownerID == playerPhotonID)
				{
					_playersTurfWar[i].content.SetActive(false);
					return;
				}
			}
		}		
	}

	public void ClearRoundUI(GameMode mode)
	{
		if (mode == GameMode.TurfWar)
		{
			for(int i =0; i< _numPlayers; i++)
			{
				_playersTurfWar[i].tilesText.text = "0";
				_playersTurfWar[i].respawnHandle.IsRunning = false;
				_playersTurfWar[i].respawnParent.SetActive(false);
			}
		}
	}

	public void SetRespawnUI(int playerPhotonID, double delta, GameMode mode)
	{
		if (mode == GameMode.TurfWar)
		{
			for (int i = 0; i < _numPlayers; i++)
			{
				if (_playersTurfWar[i].ownerID == playerPhotonID)
				{
					_playersTurfWar[i].respawnHandle = Timing.RunCoroutine(_HandleRespawnUI(playerPhotonID, i, delta, mode));
					return;
				}
			}
		}
	}
	
	IEnumerator<float> _HandleRespawnUI(int playerID, int playerIndex, double delta,  GameMode mode)
	{
		double timer = 0.0;

		if (mode == GameMode.TurfWar) timer = Match.instance.gameModeModel.turfRespawnTime;

		if (Constants.onlineGame)
			timer -= (PhotonNetwork.time - delta);

		while (timer > 0)
		{
			timer -= Time.deltaTime;
			if (mode == GameMode.TurfWar)
			{
				_playersTurfWar[playerIndex].respawnParent.SetActive(true);
				_playersTurfWar[playerIndex].respawnTimeText.text = timer.ToString("0");
			}

			yield return Timing.WaitForOneFrame;
		}

		if (mode == GameMode.TurfWar)		
			_playersTurfWar[playerIndex].respawnParent.SetActive(false);
					
	}
}

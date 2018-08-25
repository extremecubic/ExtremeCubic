using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ScoreUI : MonoBehaviour
{
	[Serializable]
	public struct PlayerElement
	{
		public GameObject content;
		public Text scoreText;
		public Text userName;

		[HideInInspector] public int ownerID;
		[HideInInspector] public bool taken;
	}

	int _numPlayers;
	[SerializeField] PlayerElement[] _players;	

	public void Setup(int numPlayers)
	{
		_numPlayers = numPlayers;

		for (int i = 0; i < numPlayers; i++)
			_players[i].content.SetActive(true);
	}

	public void RegisterPlayer(int playerID, string name)
	{
		// loop over all 4 UI spots and use the first that is not taken
		for(int i =0; i < _numPlayers; i++)
		{
			if (!_players[i].taken)
			{
				_players[i].ownerID = playerID;
				_players[i].taken = true;
				_players[i].userName.text = name;
				return;
			}
		}
	}

	public void UpdateScore(int playerID, int score)
	{
		for (int i = 0; i < _numPlayers; i++)
		{
			if (_players[i].ownerID == playerID)
			{
				_players[i].scoreText.text = score.ToString();				
				return;
			}
		}		
	}

	public void DisableUIOfDisconnectedPlayer(int playerID)
	{
		for (int i = 0; i < _numPlayers; i++)
		{
			if (_players[i].ownerID == playerID)
			{
				_players[i].content.SetActive(false);
				return;
			}
		}		
	}

	public string GetUserNameFromID(int playerID)
	{
		for (int i = 0; i < _numPlayers; i++)
			if (_players[i].ownerID == playerID)
				return _players[i].userName.text;

		return "";										
	}
}

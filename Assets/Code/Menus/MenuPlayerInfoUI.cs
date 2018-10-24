using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public enum MenuScreen
{
	Connect,
	LevelSelect,
	CharacterSelect,
}
	
public class MenuPlayerInfoUI : Photon.MonoBehaviour
{
	[Serializable]
	public struct PlayerInfo
	{
		public GameObject content;		
		public Text nickName;
		public Image checkMark;

		[HideInInspector] public int ownerID;
		[HideInInspector] public bool taken;
		[HideInInspector] public int index;
	}

	[SerializeField] PlayerInfo[] _players;

	[Header("SPECIFIC SCREEN TRANSFORM POINTS")]
	[SerializeField] Transform[] _connectScreen;
	[SerializeField] Transform[] _levelSelectScreen;
	[SerializeField] Transform[] _characterSelectScreen;

	Transform[][] _screenTransforms = new Transform[3][];

	void Awake()
	{
		// set all different transform the player info UI
		// has depending of witch screen is open
		_screenTransforms[0] = _connectScreen;
		_screenTransforms[1] = _levelSelectScreen;
		_screenTransforms[2] = _characterSelectScreen;
	}

	// a player will claim a UI spot in array
	// and is later accesed by their photon player id
	[PunRPC]
	void ClaimUIBox(int ID, string nickName, string CharacterName)
	{
		for (int i = 0; i < 4; i++)
			if (!_players[i].taken)
			{
				_players[i].content.SetActive(true);
				_players[i].ownerID = ID;
				_players[i].taken = true;
				_players[i].nickName.text = nickName;				
				_players[i].index = i;
				return;
			}
	}

	// remove UI of a disconnected player
	[PunRPC]
	public void DisableUIOfPlayer(int ID)
	{
		for (int i = 0; i < 4; i++)
			if (_players[i].ownerID == ID)
			{
				_players[i].content.SetActive(false);
				_players[i].ownerID = -99;
				_players[i].taken = false;
				_players[i].nickName.text = "";				
				_players[i].checkMark.color = new Color(1, 1, 1, 0.1f);
				return;
			}
	}

	// set the checkmark that a player has selected
	// whatever needs to be selected depending on the
	// menu screen that is active
	[PunRPC]
	void SetReadyUI(int ID, bool active)
	{
		for (int i = 0; i < 4; i++)
			if (_players[i].ownerID == ID)
			{
				_players[i].checkMark.color = active ? Color.white : new Color(1, 1, 1, 0.1f);
				return;
			}
	}

	// disable the ui of all players
	public void DisableAllPlayerUI()
	{
		for (int i = 0; i < 4; i++)			
		{
			_players[i].content.SetActive(false);
			_players[i].ownerID = -99;
			_players[i].taken = false;
			_players[i].nickName.text = "";			
			_players[i].checkMark.color = new Color(1, 1, 1, 0.1f);
		}
	}

	// get witch index in array a player
	// with a specific photon player ID have
	// used to change witch 3d model 
	public int GetArrayIndexFromID(int ID)
	{
		for (int i = 0; i < 4; i++)
			if (_players[i].ownerID == ID)
				return _players[i].index;

		return 0;
	}

	// will change the position of the player Info
	// UI based on the Type of menu screen that is active
	public void SetPlayerUIByScreen(MenuScreen screen)
	{
		for(int i =0; i < 4; i++)
		{
			_players[i].content.GetComponent<RectTransform>().anchorMin = _screenTransforms[(int)screen][i].GetComponent<RectTransform>().anchorMin;
			_players[i].content.GetComponent<RectTransform>().anchorMax = _screenTransforms[(int)screen][i].GetComponent<RectTransform>().anchorMax;
			_players[i].content.transform.position = _screenTransforms[(int)screen][i].position;
		}	
	}

}

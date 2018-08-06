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
		_screenTransforms[0] = _connectScreen;
		_screenTransforms[1] = _levelSelectScreen;
		_screenTransforms[2] = _characterSelectScreen;
	}

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

	[PunRPC]
	void UpdatePlayerUI(int ID, string characterName)
	{
		for (int i = 0; i < 4; i++)
			if (_players[i].ownerID == ID)
			{
				// will probably uppdate a 2d icon here later
				return;
			}
	}

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

	public int GetModelTransformIndexFromID(int ID)
	{
		for (int i = 0; i < 4; i++)
			if (_players[i].ownerID == ID)
				return _players[i].index;

		return 0;
	}

	public void SetPlayerUIByScreen(MenuScreen screen)
	{
		for(int i =0; i < 4; i++)		
			_players[i].content.transform.position = _screenTransforms[(int)screen][i].position;		
	}

}

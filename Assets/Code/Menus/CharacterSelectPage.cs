
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class CharacterSelectPage : MenuPage
{
	[Serializable]
	public struct CharacterButton
	{
		public Button button;
		public GameObject border;
	}

	[Header("UI REFERENCES"), Space(2)]
	[SerializeField] MenuPlayerInfoUI  _playerInfo;
	[SerializeField] CharacterButton[] _characterButtons;

	[Space(5)]
	[SerializeField] Button         _readyButton;
	[SerializeField] Button         _leaveButton;
	[SerializeField] RectTransform  _dotsParent;
	[SerializeField] Image          _dotPrefab;
	[SerializeField] MessagePromt   _promt;
	[SerializeField] Button         _leftarrow;
	[SerializeField] Button         _rightArrow;
	[SerializeField] StartCounterUI _counter;

	[Header("3D MODEL SETTINGS"),Space(2)]
	[SerializeField] Transform[]  _modelTransforms;
	[SerializeField] float        _rotationSpeed = 1.0f;
	[SerializeField] GameObject[] _characterRenders;

	CharacterDatabase.ViewData _currentView;
	GameObject[] _currentViewObject = new GameObject[4];
	
	Vector3 _rotation;
	int     _numSkins;
	int     _currentSkin;
	bool    _imReady;
	int     _currentPressedIndex = 0;

	public void OnCharacterSelcted(int buttonIndex)
	{
		// change boarder				
		_characterButtons[_currentPressedIndex].border.gameObject.SetActive(false);	
		_characterButtons[buttonIndex].border.gameObject.SetActive(true);		

		_currentPressedIndex = buttonIndex;
	}

	public void OnCharacterSelected(string name)
	{
		// get the view from the name of selcted character
		_currentView = CharacterDatabase.instance.GetViewFromName(name);

		// always start with skin 0 on new character
		_currentSkin = 0;
		_numSkins    = _currentView.prefabs.Length;
		UpdateSkinDots();

		// tell everyone to update the 3d model
		photonView.RPC("Update3DModel", PhotonTargets.All, PhotonNetwork.player.ID, name, _currentSkin);
	}

	public void OnReady()
	{
		if (_imReady)
			return;

		_imReady = true;
		ChangeAllButtonsState(false);
		EventSystem.current.SetSelectedGameObject(_leaveButton.gameObject);

		// set nickname to the name of the character for now (this will store the steam nick later instead)
		PhotonNetwork.player.NickName = _currentView.name;

		// set character chosen so we can spawn it when the game starts		
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.CHARACTER_NAME, _currentView.name);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.SKIN_ID, _currentSkin);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, true);

		// set that the game should start in online mode when the level scene loads
		Constants.onlineGame = true;

		// tell everyone to set that this player is ready in thier UI
		_playerInfo.photonView.RPC("SetReadyUI", PhotonTargets.All, PhotonNetwork.player.ID, true);		
	}

	public void OnChangeSkin(bool increment)
	{
		if (_numSkins == 0)
			return;

		_dotsParent.transform.GetChild(_currentSkin).GetComponent<Image>().color = Color.white;

		if (increment)
		{
			_currentSkin++;
			if (_currentSkin == _numSkins)
				_currentSkin = 0;
		}
		else
		{
			_currentSkin--;
			if (_currentSkin < 0)
				_currentSkin = _numSkins - 1;
		}

		_dotsParent.transform.GetChild(_currentSkin).GetComponent<Image>().color = Color.green;

		photonView.RPC("Update3DModel", PhotonTargets.All, PhotonNetwork.player.ID, _currentView.name, _currentSkin);
	}

	void UpdateSkinDots()
	{
		// remove old dots
		for (int i = 0; i < _dotsParent.transform.childCount; i++)
			Destroy(_dotsParent.GetChild(i).gameObject);

		// create new dots based on number of skins of current character
		float xPosition = 0;
		for(int i = 0; i < _numSkins; i++)
		{
			xPosition = i * 40;
			Image dot = Instantiate(_dotPrefab, _dotsParent);
			dot.GetComponent<RectTransform>().localPosition = new Vector3(xPosition, 0, 0);
			if (i == 0)
			   dot.GetComponent<Image>().color = Color.green;
		}	
	}	

	[PunRPC]
	void Update3DModel(int ID, string character, int skinID)
	{
		// update the 3d model of the player with this ID
		int index = _playerInfo.GetArrayIndexFromID(ID);

		_characterRenders[index].SetActive(true);

		// destroy old character preview model
		if (_currentViewObject[index])
			Destroy(_currentViewObject[index]);
	
		_currentViewObject[index] = Instantiate(CharacterDatabase.instance.GetViewFromName(character).prefabs[skinID], _modelTransforms[index]);
	}

	public override void OnPageEnter()
	{
		// move all player UI boxes to the prefered positions of this page
		// unselect ready arrows from last screen
		_playerInfo.SetPlayerUIByScreen(MenuScreen.CharacterSelect);
		_playerInfo.photonView.RPC("SetReadyUI", PhotonTargets.AllViaServer, PhotonNetwork.player.ID, false);

		// get the view of first model in character database
		_currentView = CharacterDatabase.instance.GetViewFromName(_characterButtons[0].button.name.ToLower());
		photonView.RPC("Update3DModel", PhotonTargets.AllViaServer, PhotonNetwork.player.ID, _currentView.name, _currentSkin);

		// get how many skins this character have and update dots
		_numSkins = _currentView.prefabs.Length;
		UpdateSkinDots();

		EventSystem.current.SetSelectedGameObject(_firstSelectable);
		_characterButtons[0].border.SetActive(true);

		// if masterclient tell averyone to start countdown timer
		if (PhotonNetwork.isMasterClient)
			photonView.RPC("StartCountdown", PhotonTargets.All, PhotonNetwork.time);
	}

	public override void OnPageExit()
	{
		ChangeAllButtonsState(true);
		_imReady = false;
	}

	public override void UpdatePage()
	{
		if (Input.GetButtonDown(Constants.BUTTON_LB + "0"))
			OnChangeSkin(false);

		if (Input.GetButtonDown(Constants.BUTTON_RB + "0"))
			OnChangeSkin(true);

		_rotation += Vector3.up * _rotationSpeed * Time.deltaTime;

		for (int i =0; i < 4; i++)
		{
			if (_currentViewObject[i] != null)
				_currentViewObject[i].transform.rotation = Quaternion.Euler(_rotation);
		}		

		CheckAllReady();
	}

	void ChangeAllButtonsState(bool enable)
	{
		for (int i = 0; i < _characterButtons.Length; i++)
			_characterButtons[i].button.interactable = enable;

		_readyButton.interactable = enable;
		_leftarrow.interactable   = enable;
		_rightArrow.interactable  = enable;
	}

	[PunRPC]
	void StartCountdown(double delta)
	{
		// if clients is lagging behind and is still left at previous page, set the page to this
		if (MainMenuSystem.instance.currentPage != this)
			MainMenuSystem.instance.SetToPage(MenuPageType.OnlineCharacterSelectScreen);

		_counter.StartCount(delta, 100, () => OnReady());
	}

	// photon callback forwarded from "MainMenuSystem.cs"
	public override void OnPlayerLeftRoom(PhotonPlayer player)
	{
		int index = _playerInfo.GetArrayIndexFromID(player.ID);

		// remove the UI of left player
		_playerInfo.DisableUIOfPlayer(player.ID);

		// remove 3d model of left player
		if (_currentViewObject[index] != null)
		{
			Destroy(_currentViewObject[index]);
			_characterRenders[index].SetActive(false);
		}

		// if we are last player left in room, show message and disconnect last player
		if (PhotonNetwork.room.PlayerCount == 1)
		{
			// stop counter right away, 
			_counter.CancelCount();

			// show the promt and call leaveroom when ok is pressed
			_promt.SetAndShow("All other players have left the room!!\n\n Returning to menu!!!", () => LeaveRoom());
		}
	}

	// called from UIbutton
	public void LeaveRoom()
	{
		// destroy 3d model
		for (int i = 0; i < 4; i++)		
			if (_currentViewObject[i] != null)
				Destroy(_currentViewObject[i]);									
		
		// remove all UI of players in room
		_counter.CancelCount();
		_playerInfo.DisableAllPlayerUI();

		// remove highlight of selected character
		if(_currentPressedIndex >= 0)					
			_characterButtons[_currentPressedIndex].border.gameObject.SetActive(false);
		
		// reset page properties		
		_currentSkin = 0;
		_currentPressedIndex = 0;

		// reset custom properties and leave room
		PhotonHelpers.ClearPlayerProperties(PhotonNetwork.player);
		PhotonNetwork.LeaveRoom();

		// set back to main page
		MainMenuSystem.instance.SetToPage(MenuPageType.StartScreen);
	}

	void CheckAllReady()
	{
		if (!PhotonNetwork.isMasterClient || PhotonNetwork.room.PlayerCount < 2)
			return;

		int playersReady = 0;
		foreach (PhotonPlayer p in PhotonNetwork.playerList)
			if (p.CustomProperties.ContainsKey(Constants.PLAYER_READY) && (bool)p.CustomProperties[Constants.PLAYER_READY])
				playersReady++;

		if (playersReady == PhotonNetwork.room.PlayerCount)
		{
			// loop over all players in room and give them a spawnpoint based on order in list			
			for (int i = 0; i < PhotonNetwork.room.PlayerCount; i++)
				PhotonHelpers.SetPlayerProperty(PhotonNetwork.playerList[i], Constants.SPAWN_ID, i);

			PhotonNetwork.LoadLevel(PhotonNetwork.player.CustomProperties[Constants.LEVEL_SCENE_NAME].ToString());
		}
	}
}

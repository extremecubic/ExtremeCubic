using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayWithFriendsPage : MenuPage
{
	[Header("MISC REFERENCES")]
	[SerializeField] MenuPlayerInfoUI _playerInfo;
	[SerializeField] MessagePromt     _promt;

	[Header("SUB PAGES")]
	[SerializeField] GameObject _generalPage;
	[SerializeField] GameObject _steamPage;

	[Header("NON STEAM UI REFERENCES")]
	[SerializeField] Text   _roomNameText;
	[SerializeField] Text   _joinRoomInput;
	[SerializeField] Button _joinRoomButton;

	[Header("SHARED UI REFERENCES")]
	[SerializeField] Button _continueButton;

	[Header("FIRST SELECTABLE OBJECT")]
	[SerializeField] GameObject _generalPageFirstSelectable;
	[SerializeField] GameObject _steamPageFirstSelectable;
	
	public void HostRoom()
	{
		if (PhotonNetwork.room != null)
		{
			Debug.Log("Trying to host when already connected to room, add so we disconnect and rehost like in random matchmaking");
			return;
		}

		// create a private room that can only be joined from invite
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.IsVisible = false;
		roomOptions.MaxPlayers = 4;
		
		// generate a random name, if we let photon create a name for us its about 100 characters long, works untill we intergrate steam
		string roomName = Random.Range(100, 9000).ToString();
		
		PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
	}

	// called from button
	public void JoinRoom()
	{
		if(_joinRoomInput.text == "")
		{
			Debug.Log("Trying to join room with empty string");
			return;
		}

		if (!PhotonNetwork.isMasterClient)
		   PhotonNetwork.JoinRoom(_joinRoomInput.text);
	}

	// called from button on server
	public void GoToLevelSelect()
	{
		PhotonNetwork.room.IsOpen = false;
		photonView.RPC("ContinueToLevelselect", PhotonTargets.All);
	}

	void OnCreatedRoom()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_roomNameText.text = PhotonNetwork.room.Name + " As Host";
	}

	void OnJoinedRoom()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		if (!PhotonNetwork.isMasterClient)
		   _roomNameText.text = PhotonNetwork.room.Name + " As Client";
				
		_playerInfo.photonView.RPC("ClaimUIBox", PhotonTargets.AllBufferedViaServer, PhotonNetwork.player.ID, "SteamNick", "??????????");
	}

	void OnPhotonJoinRoomFailed(object[] codeAndMsg)
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_promt.SetAndShow("Failed to join room!!\n" + codeAndMsg[1].ToString(), () => EventSystem.current.SetSelectedGameObject(_joinRoomButton.gameObject));
	}

	[PunRPC]
	void ContinueToLevelselect()
	{
		MainMenuSystem.instance.SetToPage(MenuPageType.OnlineLevelSelectScreen);
	}

	public override void OnPageEnter()
	{
		SetSubPageBasedOnGameVersion();

		_continueButton.interactable = false;

		// move all player UI boxes to the prefered positions of this page
		_playerInfo.SetPlayerUIByScreen(MenuScreen.Connect);
	}

	public override void UpdatePage()
	{
		if (PhotonNetwork.room == null)
		{
			_roomNameText.text = "Not Connected";
			return;
		}

		// when more then two players in room the host can chose to continue to next screen
		if (PhotonNetwork.isMasterClient && PhotonNetwork.room.PlayerCount > 1)
			_continueButton.interactable = true;
		else
			_continueButton.interactable = false;
	}

	public override void OnPageExit()
	{		
	}

	public override void OnPlayerLeftRoom(PhotonPlayer player)
	{
		_playerInfo.DisableUIOfPlayer(player.ID);
	}

	public void LeaveRoom()
	{
		// if yet not in room just return to main menu
		if(PhotonNetwork.room == null)
		{
			MainMenuSystem.instance.SetToPage(MenuPageType.StartScreen);
			return;
		}

		// if connected to room, unclaim UI and leave room before we return to main menu
		_playerInfo.DisableAllPlayerUI();
		PhotonNetwork.LeaveRoom();
		MainMenuSystem.instance.SetToPage(MenuPageType.StartScreen);
	}

	// will set witch UI That we will use
	// depending if we are running the steam version
	// and will be creating games and inviting friend through steam
	// or if we will do the invite outside of steam by just generating a code that 
	// the host manually have to send to his friends
	void SetSubPageBasedOnGameVersion()
	{
#if STEAM_VERSION
		_steamPage.SetActive(true);
		_generalPage.SetActive(false);
		EventSystem.current.SetSelectedGameObject(_steamPageFirstSelectable);
#endif

#if NO_SERVICE_VERSION
		_steamPage.SetActive(false);
		_generalPage.SetActive(true);
		EventSystem.current.SetSelectedGameObject(_generalPageFirstSelectable);
#endif
	}
}

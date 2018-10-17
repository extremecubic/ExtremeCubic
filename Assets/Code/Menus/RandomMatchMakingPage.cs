using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RandomMatchMakingPage : MenuPage
{

	[Header("Misc References")]
	[SerializeField] MenuPlayerInfoUI _playerInfo;
	[SerializeField] Dropdown		  _preferedPlayersDropdown;

	[Header("WAITING FOR PLAYERS TEXT")]
	[SerializeField] GameObject _statusTextParent;
	[SerializeField] Text       _statusText;

	[Header("COUNTER SETTINGS")]
	[SerializeField] Text _counter;
	[SerializeField] float _waitForPlayersTime = 10;

	byte _preferedPlayers;
	bool _reJoin;
	float _timer;

	public override void OnPageEnter()
	{
		// move all player UI boxes to the prefered positions of this page
		_playerInfo.SetPlayerUIByScreen(MenuScreen.Connect);
		
		EventSystem.current.SetSelectedGameObject(_firstSelectable);
	}

	public override void OnPageExit()
	{
	}

	public override void OnPlayerLeftRoom(PhotonPlayer player)
	{
		_playerInfo.DisableUIOfPlayer(player.ID);
	}

	public override void UpdatePage()
	{
		if(PhotonNetwork.room != null)
		{
			// activate UI that we are waiting for more players
			_statusTextParent.SetActive(true);
			_statusText.text = "Waiting for players";
			_counter.gameObject.SetActive(true);

			// if max players have joined the room continue to next screen without waiting for timer
			if (PhotonNetwork.room.PlayerCount == PhotonNetwork.room.MaxPlayers)			
				ContinueAndCloseRoom();			
			
			if (PhotonNetwork.isMasterClient)
				UpdateTimer();
	
			_counter.text = _timer.ToString("0");
		}
		else
		{
			_statusTextParent.SetActive(false);
			_counter.gameObject.SetActive(false);
		}	
	}

	public void JoinMatch()
	{
		// saved the prefered num players we want to play with
		_preferedPlayers = (byte)(_preferedPlayersDropdown.value + 2);

		// no perfered players to play with is stored first in list
		if(_preferedPlayersDropdown.value == 0)
			_preferedPlayers = 0;

		// if we are in room already we need to disconnect 
		// and wait for "OnConnectedToMaster" callback before we can join new
		// therefore set rejoin to true and we will request to join new room in
		// the photon callback
		if (PhotonNetwork.room != null)
		{
			_playerInfo.DisableAllPlayerUI();
			PhotonNetwork.LeaveRoom();
			_reJoin = true;
			return;
		}		

		PhotonNetwork.JoinRandomRoom(null, _preferedPlayers);
	}

	void OnJoinedRoom()
	{
		// claim a Player UI box when entering room and set countdown timer to max
		// if not master client the timer will be corrected from the server to the correct value
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_timer = _waitForPlayersTime;

		_playerInfo.photonView.RPC("ClaimUIBox", PhotonTargets.AllBufferedViaServer, PhotonNetwork.player.ID, "SteamNick", "??????????");
	}	

	void OnConnectedToMaster()
	{
		// when we leave a room for joining another one
		// we have to wait for this callback before we can
		// connect to a new room, rejoin is set to true when we
		// leave the old room for joining a new one
		if (MainMenuSystem.instance.currentPage != this)
			return;

		if (_reJoin)
		{
			PhotonNetwork.JoinRandomRoom(null, _preferedPlayers);
			_reJoin = false;
		}
	}

	void OnPhotonRandomJoinFailed(object[] codeAndMsg)
	{
		// no empty space in any room was found
		// create new room and let others join
		if (MainMenuSystem.instance.currentPage != this)
			return;

		// if we said to join first best room no matter max players in room
		// and no room was found at all, set the max players on created room to 
		// max players in game
		if (_preferedPlayers == 0)
			_preferedPlayers = 4;

		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = _preferedPlayers;

		PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
	}

	[PunRPC]
	void ContinueAndCloseRoom()
	{
		// close room and go to levelselect
		if (PhotonNetwork.isMasterClient)
			PhotonNetwork.room.IsOpen = false;

		MainMenuSystem.instance.SetToPage(Constants.SCREEN_ONLINE_LEVELSELECT);
	}

	public void OnLeave()
	{
		if (PhotonNetwork.room != null)
			PhotonNetwork.LeaveRoom();

		_playerInfo.DisableAllPlayerUI();

		MainMenuSystem.instance.SetToPage(Constants.SCREEN_START);
	}

	void UpdateTimer()
	{		
		// countdown the timer on master client
		// if more then 1 player is connected to room
		// start the game even if the prefered player count of room was higher
		_timer -= Time.deltaTime;
		if (_timer <= 0)
		{
			if (PhotonNetwork.room.PlayerCount > 1)
				photonView.RPC("ContinueAndCloseRoom", PhotonTargets.All);

			_timer = _waitForPlayersTime;
		}		
	}

	//write and read the timer
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			if (PhotonNetwork.isMasterClient)			
				stream.Serialize(ref _timer);			
		}
		else		
			stream.Serialize(ref _timer);  		
	}
}

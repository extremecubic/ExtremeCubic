using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomMatchMakingPage : MenuPage
{
	[SerializeField] MenuPlayerInfoUI _playerInfo;

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
			_statusTextParent.SetActive(true);
			_statusText.text = "Waiting for players";
			_counter.gameObject.SetActive(true);

			if (_preferedPlayers != 0 && PhotonNetwork.room.PlayerCount == PhotonNetwork.room.MaxPlayers)			
				ContinueAndCloseRoom();			
			else if(_preferedPlayers == 0 && PhotonNetwork.room.PlayerCount == 4)			
				ContinueAndCloseRoom();

			UpdateTimer();
	
			_counter.text = _timer.ToString("0");
		}
		else
		{
			_statusTextParent.SetActive(false);
			_counter.gameObject.SetActive(false);
		}	
	}

	public void JoinMatch(int preferedPlayers)
	{
		if (PhotonNetwork.room != null)
		{
			_playerInfo.DisableAllPlayerUI();
			PhotonNetwork.LeaveRoom();
			_preferedPlayers = (byte)preferedPlayers;
			_reJoin = true;
			return;
		}

		_preferedPlayers = (byte)preferedPlayers;
		PhotonNetwork.JoinRandomRoom(null, (byte)preferedPlayers);
	}

	void OnJoinedRoom()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_timer = _waitForPlayersTime;

		_playerInfo.photonView.RPC("ClaimUIBox", PhotonTargets.AllBufferedViaServer, PhotonNetwork.player.ID, "SteamNick", "??????????");
		Debug.Log("RoomJoined");
	}	

	void OnConnectedToMaster()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		// when we leave a room for joining another one
		// we have to wait for this callback before we can
		// connect to a new room, rejoin is set to true when we
		// leave the old room for joining a new one
		if (_reJoin)
		{
			PhotonNetwork.JoinRandomRoom(null, _preferedPlayers);
			_reJoin = false;
		}
	}

	void OnPhotonRandomJoinFailed(object[] codeAndMsg)
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		Debug.Log("Failed to join any room, creating new");
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = _preferedPlayers;

		PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
	}

	void ContinueAndCloseRoom()
	{
		if (PhotonNetwork.isMasterClient)
			PhotonNetwork.room.IsOpen = false;

		MainMenuSystem.instance.SetToPage(Constants.SCREEN_ONLINE_LEVELSELECT);
	}

	void UpdateTimer()
	{
		if (PhotonNetwork.isMasterClient)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				if (PhotonNetwork.room.PlayerCount > 1)
					ContinueAndCloseRoom();

				_timer = _waitForPlayersTime;
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			if (PhotonNetwork.isMasterClient)			
				stream.Serialize(ref _timer);			
		}
		else
		{
			stream.Serialize(ref _timer);  
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSystem : Photon.MonoBehaviour
{
	public static MainMenuSystem instance { get; private set; }

	public static string startPage = "StartScreen";
	public static bool reclaimPlayerUI;

	[SerializeField] MenuPlayerInfoUI _playerInfo;

	[SerializeField] MenuPage[] _menuPages;


	MenuPage _currentPage;

	void Awake()
	{
		instance = this;

		if (PhotonNetwork.connected)
			return;

		// network initialization wont be here later on
		PhotonNetwork.sendRate = 64;
		PhotonNetwork.sendRateOnSerialize = 64;

		PhotonNetwork.automaticallySyncScene = true;

		PhotonNetwork.ConnectUsingSettings(Constants.GAME_VERSION);
		
	}

	void Start()
	{
		if(reclaimPlayerUI)
			_playerInfo.photonView.RPC("ClaimUIBox", PhotonTargets.AllBufferedViaServer, PhotonNetwork.player.ID, "SteamNick", "??????????");

		SetToPage(startPage);
	}

	public void SetToPage(string pagename)
	{
		if (_currentPage != null)
			_currentPage.OnPageExit();

		for (int i =0; i < _menuPages.Length; i++)
		{
			if(_menuPages[i].pageName == pagename)
			{				
				_currentPage = _menuPages[i];
				_currentPage.EnableDisableContent(true);
				_currentPage.OnPageEnter();
				continue;
			}

			_menuPages[i].EnableDisableContent(false);
		}
	}

	void Update()
	{
		if (_currentPage == null)
			return;

		_currentPage.UpdatePage();
	}

	void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
	{
		if (_currentPage != null)
			_currentPage.OnPlayerLeftRoom(otherPlayer);
	}

}

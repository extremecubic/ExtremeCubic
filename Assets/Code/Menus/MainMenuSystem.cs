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
	public MenuPage currentPage { get; private set; }

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
		if (currentPage != null && currentPage.pageName == pagename)
			return;

		if (currentPage != null)
			currentPage.OnPageExit();

		for (int i =0; i < _menuPages.Length; i++)
		{
			if(_menuPages[i].pageName == pagename)
			{				
				currentPage = _menuPages[i];
				currentPage.EnableDisableContent(true);
				currentPage.OnPageEnter();
				continue;
			}

			_menuPages[i].EnableDisableContent(false);
		}
	}

	void Update()
	{
		if (currentPage == null)
			return;

		currentPage.UpdatePage();
	}

	void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
	{
		if (currentPage != null)
			currentPage.OnPlayerLeftRoom(otherPlayer);
	}

}

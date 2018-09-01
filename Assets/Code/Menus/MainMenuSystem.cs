using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSystem : Photon.MonoBehaviour
{
	public static MainMenuSystem instance { get; private set; }

	public static string startPage = "StartScreen";
	public static bool reclaimPlayerUI;

	[SerializeField] MenuPlayerInfoUI _playerInfo;

	[SerializeField] MenuPage[] _menuPages; public MenuPage[] menuPages { get { return _menuPages; } }
	public MenuPage currentPage { get; private set; }

	void Awake()
	{
		instance = this;	
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

		if (currentPage != null && Application.isPlaying)
			currentPage.OnPageExit();

		for (int i =0; i < _menuPages.Length; i++)
		{
			if(_menuPages[i].pageName == pagename)
			{				
				currentPage = _menuPages[i];
				currentPage.EnableDisableContent(true);

				if(Application.isPlaying)
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

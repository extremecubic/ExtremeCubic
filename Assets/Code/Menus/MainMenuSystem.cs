using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSystem : Photon.MonoBehaviour
{
	public static MainMenuSystem instance { get; private set; }

	public static MenuPageType startPage = MenuPageType.StartScreen;
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
		if (reclaimPlayerUI)
			_playerInfo.photonView.RPC("ClaimUIBox", PhotonTargets.AllViaServer, PhotonNetwork.player.ID, "SteamNick", "??????????");

		SetToPage(startPage);
	}

	public void SetToPageFromPageID(int ID)
	{
		SetToPage((MenuPageType)ID);
	}

	// will change to a new menu page
	public void SetToPage(MenuPageType pageType)
	{
		// if trying to change to the same page that is active, return
		if (currentPage != null && currentPage.pageType == pageType)
			return;

		// dont do any call to pages if this is called from the editor
		// else call on exit on the current page before we change
		if (currentPage != null && Application.isPlaying)
			currentPage.OnPageExit();

		// loop over pages untill we find the one
		// we want to transition to
		for (int i =0; i < _menuPages.Length; i++)
		{
			if(_menuPages[i].pageType == pageType)
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

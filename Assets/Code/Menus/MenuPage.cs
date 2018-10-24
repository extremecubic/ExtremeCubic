using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MenuPageType : int
{
	StartScreen,
	OnlinePlayScreen,
	OnlinePlayWithFriendsScreen,
	OnlineLevelSelectScreen,
	OnlineCharacterSelectScreen,
	OnlineRandomMatchMakingScreen,
}

// base class of a menu page
// this functions will then always
// be called on the page that is active
public abstract class MenuPage : Photon.MonoBehaviour
{
	[SerializeField] protected GameObject _content;
	[SerializeField] protected GameObject _firstSelectable;
	[SerializeField] protected MenuPageType _pageType; public MenuPageType pageType { get { return _pageType; } }

	public abstract void OnPageEnter();
	public abstract void UpdatePage();
	public abstract void OnPageExit();
	public abstract void OnPlayerLeftRoom(PhotonPlayer player);

	public void EnableDisableContent(bool enable)
	{
		_content.SetActive(enable);
	}
}

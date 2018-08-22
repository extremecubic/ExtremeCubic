using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnlinePlayPage : MenuPage
{
	[SerializeField] GameObject _connectingParent;
	[SerializeField] MessagePromt _promt;

	[SerializeField] Button[] _buttons;

	public override void OnPageEnter()
	{
		if (PhotonNetwork.connected)
			return;

		for (int i = 0; i < _buttons.Length; i++)
			_buttons[i].interactable = false;

		_connectingParent.SetActive(true);

		PhotonNetwork.sendRate = 64;
		PhotonNetwork.sendRateOnSerialize = 64;

		PhotonNetwork.automaticallySyncScene = true;

		PhotonNetwork.ConnectUsingSettings(Constants.GAME_VERSION);		
	}

	public override void OnPageExit()
	{
		
	}

	public override void OnPlayerLeftRoom(PhotonPlayer player)
	{
		
	}

	public override void UpdatePage()
	{
		
	}

	void OnConnectedToMaster()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_connectingParent.SetActive(false);

		for (int i = 0; i < _buttons.Length; i++)
			_buttons[i].interactable = true;
	}

	void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_connectingParent.SetActive(false);
		_promt.SetAndShow(string.Format("Failed to Connect to Server!\nError : {0}", cause.ToString()), () => MainMenuSystem.instance.SetToPage(Constants.SCREEN_START));
	}


}

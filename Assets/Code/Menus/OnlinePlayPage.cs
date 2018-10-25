using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnlinePlayPage : MenuPage
{
	[SerializeField] GameObject _connectingParent;
	[SerializeField] MessagePromt _promt;

	[SerializeField] Button[] _buttons;
	[SerializeField] Button _returnButton;

	// connect to photon if we are not already connected
	public override void OnPageEnter()
	{
		if (PhotonNetwork.connected)
		{
			EventSystem.current.SetSelectedGameObject(_firstSelectable);
			return;
		}

		// buttons will be inactive untill we are connected
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

	// will enable all buttons if we succesfully connected to photon
	void OnConnectedToMaster()
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_connectingParent.SetActive(false);

		for (int i = 0; i < _buttons.Length; i++)
			_buttons[i].interactable = true;

		EventSystem.current.SetSelectedGameObject(_firstSelectable);
	}

	// show a text box with error to the user that they failed
	// to connect to the photon server
	void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		if (MainMenuSystem.instance.currentPage != this)
			return;

		_connectingParent.SetActive(false);
		_promt.SetAndShow(string.Format("Failed to Connect to Server!\nError : {0}", cause.ToString()), () => 
		{			
			EventSystem.current.SetSelectedGameObject(_returnButton.gameObject);
		});
	}


}

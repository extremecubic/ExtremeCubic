using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuPage : MenuPage
{
	public override void OnPageEnter()
	{
		EventSystem.current.SetSelectedGameObject(_firstSelectable);
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
}

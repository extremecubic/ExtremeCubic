﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using UnityEngine.SceneManagement;

public class WinnerUI : MonoBehaviour
{
	[SerializeField] GameObject _content;
	[SerializeField] Text _winnerNameText;

	public void ShowWinner(string userName)
	{
		Timing.RunCoroutine(_showWinner(userName));
	}

	IEnumerator<float> _showWinner(string name)
	{
		yield return Timing.WaitForSeconds(1);

		_content.SetActive(true);
		_winnerNameText.text = name;

		yield return Timing.WaitForSeconds(3);

		if (Constants.onlineGame)
			GameOnlineOver();

		if (!Constants.onlineGame)
			GameLocalOver();		
	}
	
	void GameOnlineOver()
	{
		// set witch page to set active when returning to menu scene and that all players left in room need to claim a UIPlayerBox
		MainMenuSystem.reclaimPlayerUI = true;
		MainMenuSystem.startPage = MenuPageType.OnlineLevelSelectScreen;

		// clear all properties
		PhotonHelpers.ClearPlayerProperties(PhotonNetwork.player);

		// kill all running coroutines before we load out of this scene
		// MEC can keep trying to call a coroutine that was running in a destroyed object
		Timing.KillCoroutines();

		if (PhotonNetwork.isMasterClient)
			PhotonNetwork.LoadLevel("Menu");
	}

	void GameLocalOver()
	{
		MainMenuSystem.startPage = MenuPageType.StartScreen;
		SceneManager.LoadScene("Menu");
	}
}

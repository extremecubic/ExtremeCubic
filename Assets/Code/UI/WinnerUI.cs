using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class WinnerUI : MonoBehaviour
{
	[SerializeField] GameObject _content;
	[SerializeField] Text _winnerNameText;


	public void ShowWinner(int id)
	{
		string nick = "";
		foreach (PhotonPlayer p in PhotonNetwork.playerList)
			if (p.ID == id)
				nick = p.NickName;

		Timing.RunCoroutine(_showWinner(nick));
	}

	IEnumerator<float> _showWinner(string name)
	{
		yield return Timing.WaitForSeconds(1);

		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, false);

		_content.SetActive(true);
		_winnerNameText.text = name;

		// set witch page to set active when returning to menu scene and that all players left in room need to claim a UIPlayerBox
		MainMenuSystem.reclaimPlayerUI = true;
		MainMenuSystem.startPage = Constants.SCREEN_ONLINE_CHARACTERSELECT;

		yield return Timing.WaitForSeconds(3);

		if (PhotonNetwork.isMasterClient)
			PhotonNetwork.LoadLevel("Menu");
	}
	
}

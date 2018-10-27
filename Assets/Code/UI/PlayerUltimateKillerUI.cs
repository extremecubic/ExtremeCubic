using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class PlayerUltimateKillerUI : PlayerUIItem
{
	[Space(2), Header("TURF REFERENCES")]
	[SerializeField] Text _killsText;

	public override void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName)
	{
		ownerID = playerPhotonID;
		taken = true;

		_userNameText.text = nickName;
		_icon.sprite = CharacterDatabase.instance.GetViewFromName(viewName).iconUI;
		_scoreText.text = "0";
		_killsText.text = "0";
		_respawnParent.SetActive(false);
	}

	public override void ClearRoundUI()
	{
		_killsText.text = "0";
		_respawnHandle.IsRunning = false;
		_respawnParent.SetActive(false);
	}

	public void UpdateKillUI(int newScore)
	{
		_killsText.text = newScore.ToString();
	}
}

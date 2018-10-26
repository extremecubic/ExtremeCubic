using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// class with the specific UI for the
// King of the hill Gamemode
[Serializable]
public class PlayerKingOfTheHillUI : PlayerUIItem
{
	public override void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName)
	{
		ownerID = playerPhotonID;
		taken   = true;

		_userNameText.text = nickName;
		_icon.sprite       = CharacterDatabase.instance.GetViewFromName(viewName).iconUI;
		_scoreText.text    = "0";
	}

	public override void ClearRoundUI()
	{

	}


}

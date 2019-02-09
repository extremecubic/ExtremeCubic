using UnityEngine;
using UnityEngine.UI;
using System;

// class with the specific UI for the
// turf war Gamemode
[Serializable]
public class PlayerTurfWarUI : PlayerUIItem
{	
	[Space(2), Header("TURF REFERENCES")]
	[SerializeField] Text  _turfText;
	[SerializeField] Image _colorImage;

	public override void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName)
	{
		ownerID = playerPhotonID;
		taken = true;

		_userNameText.text = nickName;
		_icon.sprite       = CharacterDatabase.instance.GetViewFromName(viewName).iconUI;
		_scoreText.text    = "0";
		_turfText.text     = "0";
		_respawnParent.SetActive(false);
		_colorImage.color = Match.instance.gameModeModel.GetColorFromPlayerIndexID(playerIndexID);
	}

	public override void ClearRoundUI()
	{
		_turfText.text = "0";
		_respawnHandle.IsRunning = false;
		_respawnParent.SetActive(false);
	}

	public void UpdateTurfScore(int newScore)
	{
		_turfText.text = newScore.ToString();
	}
	
}

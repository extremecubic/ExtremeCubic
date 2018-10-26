using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using MEC;

// if a new type of inGame Player score UI needs
// to be implemented, inherit from this class.
// has the base functionality for the most common
// use cases and can be expanded from the inhereting class
[Serializable]
public abstract class PlayerUIItem 
{
	[Header("GENERAL")]
	[SerializeField] protected GameObject _content;        
	[SerializeField] protected Text       _scoreText;       
	[SerializeField] protected Text       _userNameText;    
	[SerializeField] protected Image      _icon;      
	
	[Space(2), Header("ONLY FOR GAMEMODES THAT HAVE RESPAWNING")]
	[SerializeField] protected GameObject _respawnParent;
	[SerializeField] protected Text       _respawnTimeText;

	protected CoroutineHandle _respawnHandle;

	public int  ownerID { get; protected set; }
	public bool taken   { get; protected set; }

	// abstract methods that require thier own implementation depending
	// on the type of UI that needs to be shown
	public abstract void RegisterPlayer(int playerPhotonID, int playerIndexID, string nickName, string viewName);
	public abstract void ClearRoundUI();

	public string GetUserName()
	{
		return _userNameText.text;
	}

	public void UpdateRoundScore(int score)
	{
		_scoreText.text = score.ToString();
	}

	public void EnableUI(bool enable)
	{
		_content.SetActive(enable);
	}

	public void SetRespawnUI(double delta, GameMode mode)
	{
		_respawnHandle = Timing.RunCoroutine(_HandleRespawnUI(delta, mode));
	}

	IEnumerator<float> _HandleRespawnUI(double delta, GameMode mode)
	{
		double timer = Match.instance.gameModeModel.GetRespawnTimeFromGameMode(mode);

		// remove the netdelta if we are playing online
		if (Constants.onlineGame)
			timer -= (PhotonNetwork.time - delta);

		// activate the UI object that has all the respawning UI
		_respawnParent.SetActive(true);

		// Update the text on when we will respawn
		while (timer > 0)
		{
			timer -= Time.deltaTime;
			
			_respawnTimeText.text = timer.ToString("0");			

			yield return Timing.WaitForOneFrame;
		}

		// set the UI inactive
		_respawnParent.SetActive(false);
	}
}

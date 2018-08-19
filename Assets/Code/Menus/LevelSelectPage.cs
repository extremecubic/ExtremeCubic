using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using MEC;

public class LevelSelectPage : MenuPage
{
	[Serializable]
	public class LevelData
	{
		public string name;
		public string sceneName;
		public Button button;
		public Sprite sprite;
	}

	[Serializable]
	public class NominatedData
	{
		public GameObject content;
		public Image image;
		public Text name;
	}

	[Header("REFERENCES")]
	[SerializeField] GameObject       _selectScreen;
	[SerializeField] GameObject       _nominatedScreen;
	[SerializeField] MenuPlayerInfoUI _playerInfo;
	[SerializeField] MessagePromt     _promt;
	[SerializeField] StartCounterUI   _counter;

	[Header("DATA STRUCTURES FOR LEVELS")]
	[SerializeField] LevelData[]     _levels;
	[SerializeField] NominatedData[] _nominatedLevelUI;

	[Header("WINNER LEVEL SCREEN SETTINGS")]
	[SerializeField] GameObject _border;
	[SerializeField] Text       _levelWinnerNameText;
	[SerializeField] float      _timePerStep = 0.04f;
	[SerializeField] float      _timeIncresePerLoop = 0.05f;
	[SerializeField] int        _numLoops = 0;
	[SerializeField] int        _loopsWithoutTimeIncrease = 12;

	CoroutineHandle _handle;

	public void OnLevelSelected(int level)
	{
		// set level ID of nominated level and set that we are ready
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, true);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.NOMINATED_LEVEL, level);
		ChangeAllButtonsState(false);

		// tell server that we are selected and ready
		_playerInfo.photonView.RPC("SetReadyUI", PhotonTargets.All, PhotonNetwork.player.ID, true);
	}

	public override void OnPageEnter()
	{
		// move all player UI boxes to the prefered positions of this page
		_playerInfo.SetPlayerUIByScreen(MenuScreen.LevelSelect);

		InvokeRepeating("AllNominatedLevel", 0, 5);
		_selectScreen.SetActive(true);
		_nominatedScreen.SetActive(false);

		// if masterclient tell everyone to start countdown timer
		if (PhotonNetwork.isMasterClient)
			photonView.RPC("StartCountdown", PhotonTargets.All, PhotonNetwork.time);
	}

	public override void OnPageExit()
	{
		ChangeAllButtonsState(true);
	}

	public override void OnPlayerLeftRoom(PhotonPlayer player)
	{
		_playerInfo.DisableUIOfPlayer(player.ID);
		if (PhotonNetwork.room.PlayerCount == 1)
		{
			Timing.KillCoroutines(_handle);
			_counter.CancelCount();
			_promt.SetAndShow("All other players have left the room!!\n\n Returning to menu!!!",
				() => {
					LeaveRoom();
				});
		}		
	}

	public override void UpdatePage()
	{		
	}

	void AllNominatedLevel()
	{
		if (!PhotonNetwork.isMasterClient || PhotonNetwork.room.PlayerCount < 2)
			return;

		// check if all players have nominated a level
		int playersReady = 0;
		foreach (PhotonPlayer p in PhotonNetwork.playerList)
			if (p.CustomProperties.ContainsKey(Constants.PLAYER_READY) && (bool)p.CustomProperties[Constants.PLAYER_READY])
				playersReady++;

		int numPlayers = PhotonNetwork.room.PlayerCount;
		if (playersReady == numPlayers)
		{
			// randomize a winning player (will play this players nominated level)
			int winner = Random.Range(0, numPlayers);
			
			// store all nominated level ID's in array
			int[] nL = { 0, 0, 0, 0 };
			string winnerLevel = "";
			string winnerLevelName = "";

			for (int i =0; i < numPlayers; i++)
			{				
				// store all nominated level ID's 
				nL[i] = (int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL];

				// if this player is the one that got randomized as winner, get the scene and level name of his nomination
				if (i == winner)
				{
					winnerLevel     = _levels[(int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL]].sceneName;
					winnerLevelName = _levels[(int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL]].name;
				}
			}

			// tell everyone to play nomination animation and set witch level to load
			photonView.RPC("LevelToPlay", PhotonTargets.All, winnerLevel, winnerLevelName, winner, nL[0], nL[1], nL[2], nL[3]);
		}
	}

	[PunRPC]
	void LevelToPlay(string level, string levelName, int winnerIndex, int one, int two, int three, int four)
	{
		// stop count and cancel to keep checking if all is selected
		_counter.CancelCount();
		CancelInvoke("AllNominatedLevel");

		// set witch level we will load later and reset ready for next screen
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.LEVEL_SCENE_NAME, level);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, false);

		// starrt the animation
		_handle = Timing.RunCoroutine(_PickRandomLevel(winnerIndex, levelName, new int[]{one, two, three, four}));
	}

	[PunRPC]
	void StartCountdown(double delta)
	{
		_counter.StartCount(delta, 20, () => OnLevelSelected(Random.Range(0, _levels.Length)));
	}

	void GoToCharacter()
	{		
		MainMenuSystem.instance.SetToPage(Constants.SCREEN_ONLINE_CHARACTERSELECT);
	}

	public void LeaveRoom()
	{
		// remove all UI
		_counter.CancelCount();
		_playerInfo.DisableAllPlayerUI();

		// clear and leave photon room
		PhotonNetwork.RemovePlayerCustomProperties(null);
		PhotonNetwork.LeaveRoom();

		// go back to main menu
		MainMenuSystem.instance.SetToPage(Constants.SCREEN_START);
	}

	void ChangeAllButtonsState(bool enable)
	{
		for (int i = 0; i < _levels.Length; i++)
			_levels[i].button.interactable = enable;
	}

	IEnumerator<float> _PickRandomLevel(int winnerIndex, string levelName, int[] nominatedLevels)
	{
		// set select screen inactive and activate nomination screen
		_nominatedScreen.SetActive(true);
		_selectScreen.SetActive(false);

		// activate border and set text of level to empty
		_border.SetActive(true);
		_levelWinnerNameText.text = "";

		// set all 4 levels to inactive (dont know how many we will have)
		for (int i = 0; i < 4; i++)
			_nominatedLevelUI[i].content.SetActive(false);

		// set levels active depending of num nominations
		int numLevels = PhotonNetwork.room.PlayerCount;
		for(int i =0; i< numLevels; i++)
		{
			// set the correct sprite of all nominated levels
			_nominatedLevelUI[i].content.SetActive(true);
			_nominatedLevelUI[i].image.sprite = _levels[nominatedLevels[i]].sprite;
		}

		// set count variables for randomize level animation
		int steps = winnerIndex + 1 + (numLevels * _numLoops);		
		int count = 0;
		int loops = 0;

		float timePerStep = _timePerStep;
		
		while (count < steps )
		{			
			for(int i =0; i < numLevels; i++)
			{					
				// sert position of border
				_border.transform.position = _nominatedLevelUI[i].content.transform.position;

				// set scale on highlighted level
				for(int y=0; y < numLevels; y++)
				{
					if (y == i)
						_nominatedLevelUI[y].content.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
					else
						_nominatedLevelUI[y].content.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
				}

				// add to count and break out if we are done
				count++;
				if (count == steps)
					break;

				yield return Timing.WaitForSeconds(timePerStep);
			}

			// increment loops and start slowing down animation if it is time
			loops++;
			if(loops >= _loopsWithoutTimeIncrease)
			  timePerStep += _timeIncresePerLoop;
		}

		// show the name of selected level
		_levelWinnerNameText.text = levelName;

		yield return Timing.WaitForSeconds(3.0f);

		// go to character screen
		GoToCharacter();
	}

}

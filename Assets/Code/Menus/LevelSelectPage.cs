using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using MEC;
using UnityEngine.EventSystems;

public class LevelSelectPage : MenuPage
{
	[Serializable]
	public class LevelData
	{
		[Header("Data")]
		public string   sceneName;
		public Sprite[] sprites;
		public string[] names;
		[NonSerialized] public int currentMap;

		[Header("UI REFERENCES")]
		public GameObject dotsParent;
		public Image	  buttonImage;
		public Text		  levelText;
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
	[SerializeField] Image            _dotPrefab;
	[SerializeField] GameObject[]     _buttonParents;
	[SerializeField] GameObject[]	  _GameModeButtonBoarders;
	[SerializeField] Button[]         _buttonsToEnableDisable;

	[Header("DATA STRUCTURES FOR LEVELS")]
	[SerializeField] LevelData[]     _kingOfTheHillLevels;
	[SerializeField] LevelData[]     _turfWarLevels;
	[SerializeField] NominatedData[] _nominatedLevelUI;

	[Header("WINNER LEVEL SCREEN SETTINGS")]
	[SerializeField] GameObject _border;
	[SerializeField] Text       _levelWinnerNameText;
	[SerializeField] float      _timePerStep = 0.04f;
	[SerializeField] float      _timeIncresePerLoop = 0.05f;
	[SerializeField] int        _numLoops = 0;
	[SerializeField] int        _loopsWithoutTimeIncrease = 12;

	LevelData[][] _levels;

	CoroutineHandle _handle;

	int  _levelToChangeMap;
	bool _randomizeMap;
	int  _currentGameModeIndex;

	public void OnLevelSelected(int level)
	{	
		int mapID = _levels[_currentGameModeIndex][level].currentMap;

		// if timer have run out we want to randomize witch map of the level that will be played
		if (_randomizeMap)
			mapID = Random.Range(0, _levels[_currentGameModeIndex][level].sprites.Length);

		// set that we are ready
		// set the level scene to load
		// set the map to use
		// set the game mode this level belongs to
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, true);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.NOMINATED_LEVEL, level);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.NOMINATED_LEVEL_TILEMAP, mapID);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.NOMINATED_LEVEL_GAME_MODE_INDEX, _currentGameModeIndex);

		// disable all buttons
		ChangeAllButtonsState(false);

		// tell server that we are selected and ready
		_playerInfo.photonView.RPC("SetReadyUI", PhotonTargets.All, PhotonNetwork.player.ID, true);		
	}

	public override void OnPageEnter()
	{
		// fill levels 2d array with all different game mode levels
		// if this is the first time opening page
		if (_levels == null)
		{
			_levels = new LevelData[2][];

			_levels[0] = _kingOfTheHillLevels;
			_levels[1] = _turfWarLevels; 
		}

		// move all player UI boxes to the prefered positions of this page
		_playerInfo.SetPlayerUIByScreen(MenuScreen.LevelSelect);
	
		_randomizeMap         = false;
		_currentGameModeIndex = 0;

		_selectScreen.SetActive(true);
		_nominatedScreen.SetActive(false);

		// display levels from first gamemode when entering screen
		// this will also setup the rest of the necessary UI
		OnGameModeChanged(0);

		// if masterclient tell everyone to start countdown timer
		if (PhotonNetwork.isMasterClient)
			photonView.RPC("StartCountdown", PhotonTargets.All, PhotonNetwork.time);
	}

	public void OnGameModeChanged(int index)
	{
		_levelToChangeMap = 0;
		_currentGameModeIndex = index;

		// set selectable to first level
		EventSystem.current.SetSelectedGameObject(_firstSelectable);

		// enable/disable the boarder of gamemode buttons to show the selected one
		for (int i = 0; i < _GameModeButtonBoarders.Length; i++)
			_GameModeButtonBoarders[i].SetActive(i == index);

		SetUpLevelUI();
	}

	void SetUpLevelUI()
	{
		int numLevels = _levels[_currentGameModeIndex].Length;

		// enable disable level buttons depending on how many levels this game mode have
		for (int i = 0; i< 6; i++)		
			_buttonParents[i].SetActive(i < numLevels);

		// change sprite and name on all levels to match the new gamemode 
		foreach (LevelData level in _levels[_currentGameModeIndex])
		{
			level.buttonImage.sprite = level.sprites[level.currentMap];
			level.levelText.text     = level.names[level.currentMap];
		}

		for (int i =0; i < numLevels; i++)
		{
			// destroy all map dots from last gamemode
			for (int y = 0; y < _levels[_currentGameModeIndex][i].dotsParent.transform.childCount; y++)
				Destroy(_levels[_currentGameModeIndex][i].dotsParent.transform.GetChild(y).gameObject);

			// spawn new map dots depending on how many different maps the level have
			float xPosition = 0;
			for (int y = 0; y < _levels[_currentGameModeIndex][i].sprites.Length; y++)
			{
				xPosition = y * 40;
				Image dot = Instantiate(_dotPrefab, _levels[_currentGameModeIndex][i].dotsParent.transform);
				dot.GetComponent<RectTransform>().localPosition = new Vector3(xPosition, 0, 0);
				if (y == 0)
					dot.GetComponent<Image>().color = Color.green;
			}
		}		
	}

	// called from arrowbuttons before "OnMapChange" is called
	public void SetLevelIndex(int index)
	{
		_levelToChangeMap = index;
	}

	// called from arrow buttons
	public void OnChangeMap(bool increment)
	{
		LevelData lvlData = _levels[_currentGameModeIndex][_levelToChangeMap];

		if (lvlData.sprites.Length == 1)
			return;

		lvlData.dotsParent.transform.GetChild(lvlData.currentMap).GetComponent<Image>().color = Color.white;

		if (increment)
		{
			lvlData.currentMap++;
			if (lvlData.currentMap == lvlData.sprites.Length)
				lvlData.currentMap = 0;
		}
		else
		{
			lvlData.currentMap--;
			if (lvlData.currentMap < 0)
				lvlData.currentMap = lvlData.sprites.Length - 1;
		}

		lvlData.dotsParent.transform.GetChild(lvlData.currentMap).GetComponent<Image>().color = Color.green;
		lvlData.buttonImage.sprite = lvlData.sprites[lvlData.currentMap];
		lvlData.levelText.text     = lvlData.names[lvlData.currentMap];
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
				() => 
				{
					LeaveRoom();
				});
		}		
	}

	public override void UpdatePage()
	{
		if (Input.GetButtonDown(Constants.BUTTON_LB + "0"))
			FindSelectedButtonIDAndChangeMap(false);

		if (Input.GetButtonDown(Constants.BUTTON_RB + "0"))
			FindSelectedButtonIDAndChangeMap(true);

		AllNominatedLevel();
	}

	void FindSelectedButtonIDAndChangeMap(bool increment)
	{
		GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

		// will get the button index from the parent of the selected button
		// this only have to be done on controller, mouse click will send correct index
		// THIS SHOULD BE REDONE IN A BETTER WAY BUT IT WORKS FOR NOW
		if (selectedObject != null)
		{
			_levelToChangeMap = selectedObject.transform.parent.GetSiblingIndex();
			OnChangeMap(increment);
		}
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
			int[] nominatedLevels         = { 0, 0, 0, 0 };
			int[] nominatedLevelMaps      = { 0, 0, 0, 0 };
			int[] nominatedLevelGameModes = { 0, 0, 0, 0 };

			string winnerLevel     = "";
			string winnerLevelName = "";
			int    winnerLevelMap  = 0;

			for (int i =0; i < numPlayers; i++)
			{				
				// store all nominated level ID's and map ID´s
				nominatedLevels[i]         = (int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL];
				nominatedLevelMaps[i]      = (int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL_TILEMAP];
				nominatedLevelGameModes[i] = (int)PhotonNetwork.playerList[i].CustomProperties[Constants.NOMINATED_LEVEL_GAME_MODE_INDEX];

				// if this player is the one that got randomized as winner, get the scene and level name of his nomination
				if (i == winner)
				{
					winnerLevel     = _levels[_currentGameModeIndex][nominatedLevels[i]].sceneName;
					winnerLevelName = _levels[_currentGameModeIndex][nominatedLevels[i]].names[nominatedLevelMaps[i]];
					winnerLevelMap  = nominatedLevelMaps[i];
				}
			}

			// tell everyone to play nomination animation and set witch level and map to load
			photonView.RPC("LevelToPlay", PhotonTargets.All, winnerLevel, winnerLevelName, winnerLevelMap, winner, 
				nominatedLevels[0],         nominatedLevels[1],         nominatedLevels[2],         nominatedLevels[3], 
				nominatedLevelMaps[0],      nominatedLevelMaps[1],      nominatedLevelMaps[2],      nominatedLevelMaps[3],
				nominatedLevelGameModes[0], nominatedLevelGameModes[1], nominatedLevelGameModes[2], nominatedLevelGameModes[3]);
		}
	}

	[PunRPC]
	void LevelToPlay(string level, string levelName, int levelMap, int winnerIndex, 
		int Lone, int Ltwo, int Lthree, int Lfour, 
		int Mone, int Mtwo, int Mthree, int Mfour, 
		int Gone, int Gtwo, int Gthree, int Gfour)
	{
		// stop count and cancel to keep checking if all is selected
		_counter.CancelCount();		

		// set witch level we will load later and reset ready for next screen
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.LEVEL_SCENE_NAME, level);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.NOMINATED_LEVEL_MAP_INDEX, levelMap);
		PhotonHelpers.SetPlayerProperty(PhotonNetwork.player, Constants.PLAYER_READY, false);

		// start the animation
		_handle = Timing.RunCoroutine(_PickRandomLevel(winnerIndex, levelName, new int[]{Lone, Ltwo, Lthree, Lfour}, new int[] { Mone, Mtwo, Mthree, Mfour }, new int[] { Gone, Gtwo, Gthree, Gfour }));
	}

	[PunRPC]
	void StartCountdown(double delta)
	{
		_counter.StartCount(delta, 60, () =>
		{
			_randomizeMap = true;
			OnLevelSelected(Random.Range(0, _levels[_currentGameModeIndex].Length));			
		});
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
		PhotonHelpers.ClearPlayerProperties(PhotonNetwork.player);
		PhotonNetwork.LeaveRoom();

		// go back to main menu
		MainMenuSystem.instance.SetToPage(Constants.SCREEN_START);
	}

	void ChangeAllButtonsState(bool enable)
	{
		foreach (Button button in _buttonsToEnableDisable)
			button.interactable = enable;
	}

	IEnumerator<float> _PickRandomLevel(int winnerIndex, string levelName, int[] nominatedLevels, int[] nominatedMaps, int[] nominatedModes)
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
			_nominatedLevelUI[i].image.sprite = _levels[nominatedModes[i]][nominatedLevels[i]].sprites[nominatedMaps[i]];
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

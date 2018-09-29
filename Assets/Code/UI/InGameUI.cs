using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
	[SerializeField] ScoreUI        _scoreUI;        public ScoreUI scoreUI               { get { return _scoreUI; } }
	[SerializeField] StartCounterUI _startCounterUI; public StartCounterUI startCounterUI { get { return _startCounterUI; } }
	[SerializeField] WinnerUI       _winnerUI;       public WinnerUI winnerUI             { get { return _winnerUI; } }
	[SerializeField] MessagePromt   _msgPromt;       public MessagePromt msgPromt         { get { return _msgPromt; } }

	public static InGameUI instance { get; private set; }

	void Awake()
	{
		instance = this;	
	}

	void OnDestroy()
	{
		instance = null;	
	}

}

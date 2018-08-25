using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameMode 
{
	void OnPlayerDie(int ID);
	void OnSetup(int numPlayers);
	void OnRoundRestarted();
	void OnPlayerLeft(int ID);
	void OnPlayerRegistred(int ID);	
}

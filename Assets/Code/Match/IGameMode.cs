using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameMode 
{
	void OnPlayerDie(int killedPlayerID, int killerID);
	void OnSetup(int numPlayers);
	void OnRoundRestarted();
	void OnPlayerLeft(int ID);
	void OnPlayerRegistred(int ID);
	void OnRoundStart();
	void OnLevelCreated();
}

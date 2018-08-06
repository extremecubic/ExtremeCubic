using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// THIS CLASS IS USED TO START A GAME DIRECTLY FROM THE MAIN SCENE
// EVERYONE WHO STARTS THE GAME FROM THIS SCENE WILL JOIN THE SAME ROOM AND GAME
public class SimpleNetworkStarter : Photon.MonoBehaviour
{
	[SerializeField] int _spawnPoint;
	[SerializeField] string _character;
	
    void Start()
    {
        PhotonNetwork.sendRate = 64;
        PhotonNetwork.sendRateOnSerialize = 64;
		
		ConnectToServer();
    }

    void ConnectToServer() =>
        PhotonNetwork.ConnectUsingSettings(Constants.GAME_VERSION);
  
    void OnConnectedToMaster() =>
        JoinRoom();

    void JoinRoom() =>
        PhotonNetwork.JoinOrCreateRoom("debugRoomName", new RoomOptions(), TypedLobby.Default);   

    void OnJoinedRoom()
    {
		PhotonNetwork.player.NickName = "DunderBög";

		PhotonNetwork.SetPlayerCustomProperties(new Hashtable(3));
		Hashtable playerProps = PhotonNetwork.player.CustomProperties;

		playerProps.Add(Constants.CHARACTER_NAME, _character);
		playerProps.Add(Constants.SPAWN_ID, _spawnPoint);
		playerProps.Add(Constants.SKIN_ID, 0);
		
		PhotonNetwork.player.SetCustomProperties(playerProps);

		Match.instance.SimpleStart();
	}

	void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
	{
		
	}



	public static float CalculateNetDelta(double inTimestamp) =>
        (float)(PhotonNetwork.time - inTimestamp);
	
}

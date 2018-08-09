using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : Photon.MonoBehaviour
{
	public PowerUpModel _powers;

    public TileMap tileMap { get; private set; }
	
    [SerializeField] GameObject _characterPrefab; // The character gameobject that Photon automagically creates 
	[SerializeField] string     _mapToLoad;
	[SerializeField] Transform  _tilesFolder;
	[SerializeField] Transform  _powerUpFolder;

    Character _character;

	int _spawnID;

	public void ManualStart()
	{
		// get player properties saved in photonplayer
		string characterName = PhotonNetwork.player.CustomProperties[Constants.CHARACTER_NAME].ToString();
		int skinID           = (int)PhotonNetwork.player.CustomProperties[Constants.SKIN_ID];

		_character = PhotonNetwork.Instantiate("Character", Vector3.zero, Quaternion.identity, 0).GetComponent<Character>();
		_character.Initialize(characterName, PhotonNetwork.player.ID, PhotonNetwork.player.NickName, skinID);

		tileMap = new TileMap(_mapToLoad, _tilesFolder, _powerUpFolder);

		_spawnID = (int)PhotonNetwork.player.CustomProperties[Constants.SPAWN_ID];

		_character.Spawn(tileMap.GetSpawnPointFromSpawnID(_spawnID));

		for (int i = 0; i < 3; i++)
			tileMap.GetTile(tileMap.GetRandomTileCoords()).SpawnPowerUp(_powers.GetPowerUpFromType(PowerUpType.SuperSpeed), _powerUpFolder);

		for (int i = 0; i < 3; i++)
			tileMap.GetTile(tileMap.GetRandomTileCoords()).SpawnPowerUp(_powers.GetPowerUpFromType(PowerUpType.InfiniteDash), _powerUpFolder);
	}
		
	public void ResetRound()
	{
		photonView.RPC("NetworkResetRound", PhotonTargets.All);
	}

	[PunRPC]
	void NetworkResetRound()
	{
		tileMap.ResetMap();
		_character.Spawn(tileMap.GetSpawnPointFromSpawnID(_spawnID));
	}

	public void BreakTile(int x, int y)
	{
		photonView.RPC("NetworkBreakTile", PhotonTargets.All, x, y);
	}

	[PunRPC]
	void NetworkBreakTile(int x, int y)
	{
		tileMap.GetTile(new Vector2DInt(x, y)).DamageTile();
	}
}

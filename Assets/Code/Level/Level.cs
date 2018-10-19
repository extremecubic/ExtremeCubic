﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : Photon.MonoBehaviour
{
	public TileMap tileMap { get; private set; }

	[SerializeField] string _mapToLoad;
	[SerializeField] int _mapID;
	[SerializeField] Transform _tilesFolder;
	[SerializeField] Transform _powerUpFolder;
	[SerializeField] GameObject _characterPrefab;

	[Header("LEVEL SPECIFIC DEATH FEEDBACK ON EDGES AND EMPTY TILES")]
	[SerializeField] ParticleSystem _emptyDeathParticle;   public ParticleSystem emptyDeathParticle { get { return _emptyDeathParticle; } }
	[SerializeField] AudioClip      _emptyDeathSound;      public AudioClip emptyDeathsound         { get { return _emptyDeathSound; } }
	[SerializeField] DeathType      _deathType;            public DeathType deathType               { get { return _deathType; } }
	[SerializeField] Transform      _flyToTargetTransform; public Transform flyToTargetTransform    { get { return _flyToTargetTransform; } }

    List<Character> _characters = new List<Character>();

	public static Level instance { get; private set; }

	void Awake()
	{
		instance = this;	
	}

	void OnDestroy()
	{
		instance = null;
	}

	public void StartGameOnline()
	{
		// get player properties saved in photonplayer
		string characterName = PhotonNetwork.player.CustomProperties[Constants.CHARACTER_NAME].ToString();
		int skinID           = (int)PhotonNetwork.player.CustomProperties[Constants.SKIN_ID];
		int spawnID          = (int)PhotonNetwork.player.CustomProperties[Constants.SPAWN_ID];
		int mapID            = (int)PhotonNetwork.player.CustomProperties[Constants.NOMINATED_LEVEL_MAP_INDEX];

		tileMap = new TileMap(_mapToLoad + mapID.ToString(), _tilesFolder, _powerUpFolder);

		_characters.Add(PhotonNetwork.Instantiate("Character", Vector3.zero, Quaternion.identity, 0).GetComponent<Character>());
		_characters[0].Initialize(characterName, PhotonNetwork.player.ID, PhotonNetwork.player.NickName, skinID, spawnID);
		_characters[0].Spawn();
	}

	public void StartGameLocal()
	{
		// just spawn 4 duplo right now
		// this will change once menus for starting local play is done
		tileMap = new TileMap(_mapToLoad + _mapID.ToString(), _tilesFolder, _powerUpFolder);

		for(int i =0; i< 4; i++)
		{
			_characters.Add(Instantiate(_characterPrefab, Vector3.zero, Quaternion.identity).GetComponent<Character>());
			_characters[i].Initialize("duplo", i, "LocalGuy", 0, i);
			_characters[i].Spawn();
		}		
	}

	public void ResetRound()
	{
		if (Constants.onlineGame)
		    photonView.RPC("NetworkResetRound", PhotonTargets.All);

		if (!Constants.onlineGame)
			NetworkResetRound();
	}

	public void BreakTile(int x, int y)
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkBreakTile", PhotonTargets.All, x, y);

		if (!Constants.onlineGame)
			NetworkBreakTile(x, y);
	}

	[PunRPC]
	void NetworkResetRound()
	{
		tileMap.ResetMap();
		for (int i =0; i < _characters.Count; i++)
		     _characters[i].Spawn();
	}

	[PunRPC]
	void NetworkBreakTile(int x, int y)
	{
		tileMap.GetTile(new Vector2DInt(x, y)).DamageTile();
	}
}
